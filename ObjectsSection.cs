using System.Diagnostics;

namespace Salmon.Levels;

/// <summary>Stores the level object tree and editor selection state.</summary>
public sealed class ObjectsSection : LevelSection
{
    /// <summary>The root of the level object hierarchy.</summary>
    public Group Root = new();
    /// <summary>The identifier of the group currently open in the editor.</summary>
    public ulong GroupID = 0;
    /// <summary>The identifiers of the objects selected in the editor.</summary>
    public ulong[] SelectedIDs = [];
    /// <inheritdoc/>
    public override void Write(BinaryWriter writer)
    {
        DynamicSerializer.Serialize(Root, writer);
        writer.Write(GroupID);
        writer.Write(SelectedIDs.Length);
        foreach (var id in SelectedIDs)
            writer.Write(id);
    }

    /// <inheritdoc/>
    public override void Read(BinaryReader reader)
    {
        Root = (Group)DynamicSerializer.Deserialize(typeof(Group), reader);
        NormalizeCustomMaterialNames();
        if (Version >= 20)
        {
            GroupID = reader.ReadUInt64();
            var count = reader.ReadInt32();
            SelectedIDs = new ulong[count];
            for (var i = 0; i < count; i++)
                SelectedIDs[i] = reader.ReadUInt64();
        }
        if (Version < 21)
        {
            var idMap = new Dictionary<ulong, ulong>();
            RegenerateIDs(Root, idMap);
            if (idMap.TryGetValue(GroupID, out var groupID))
                GroupID = groupID;
            for (var i = 0; i < SelectedIDs.Length; i++)
                if (idMap.TryGetValue(SelectedIDs[i], out var selectedID))
                    SelectedIDs[i] = selectedID;
            var parents = new Dictionary<ObjectDefinition, Group>();
            parents[Root] = null;
            foreach (var obj in Root.Recurse())
                if (obj is Group group)
                    foreach (var child in group.Objects)
                        parents[child] = group;
            foreach (var obj in Root.Recurse())
            {
                var dynamicType = TypeCache.Get(obj.GetType());
                foreach (var stringField in dynamicType.Fields)
                {
                    if (stringField.ValueType != typeof(string))
                        continue;
                    foreach (var referenceField in dynamicType.Fields)
                    {
                        if (referenceField.ValueType != typeof(ObjectReferences) || referenceField.Label != stringField.Label)
                            continue;
                        var stringValue = (string)stringField.GetValue(obj);
                        var value = (ObjectReferences)referenceField.GetValue(obj);
                        value = ResolveMany(obj, stringValue, parents);
                        referenceField.SetValue(obj, value);
                    }
                }
            }

            static void RegenerateIDs(ObjectDefinition obj, Dictionary<ulong, ulong> idMap)
            {
                var oldID = obj.ID;
                obj.ID = Snowflake.CreateULong();
                idMap[oldID] = obj.ID;
                if (obj is not Group group || group.Objects == null)
                    return;
                foreach (var child in group.Objects)
                    if (child != null)
                        RegenerateIDs(child, idMap);
            }

            static ObjectReferences ResolveMany(ObjectDefinition source, string manyPaths, Dictionary<ObjectDefinition, Group> parents)
            {
                var references = new ObjectReferences();
                if (source == null || string.IsNullOrWhiteSpace(manyPaths))
                    return references;
                var paths = manyPaths.Split(',');
                foreach (var path in paths)
                    if (TryResolve(source, path, parents, out var target))
                        references.IDs.Add(target.ID);
                return references;
            }

            static bool TryResolve(ObjectDefinition source, string path, Dictionary<ObjectDefinition, Group> parents, out ObjectDefinition target)
            {
                target = null;
                if (source == null || string.IsNullOrWhiteSpace(path))
                    return false;
                if (!parents.TryGetValue(source, out var group))
                    return false;

                ObjectDefinition selected = null;
                var parts = path.Split('.');
                var start = 0;
                var firstPart = parts[0].Trim();
                if (firstPart == "$")
                {
                    group = parents.Keys.OfType<Group>().FirstOrDefault(group => parents[group] == null);
                    start = 1;
                }
                else if (IsParentPath(firstPart))
                {
                    for (var climb = 0; climb < firstPart.Length; climb++)
                        if (group == null || !parents.TryGetValue(group, out group))
                            return false;
                    start = 1;
                }
                for (var i = start; i < parts.Length; i++)
                {
                    var part = parts[i].Trim();
                    if (part.Length == 0)
                        return false;
                    if (group == null)
                        return false;

                    selected = FindChild(group, part);
                    if (selected == null)
                        return false;
                    group = selected as Group;
                }

                target = selected;
                return target != null;
            }

            static bool IsParentPath(string part)
            {
                if (part.Length == 0)
                    return false;
                for (var i = 0; i < part.Length; i++)
                    if (part[i] != '^')
                        return false;
                return true;
            }

            static ObjectDefinition FindChild(Group group, string name)
            {
                if (group.Objects == null)
                    return null;
                for (var i = 0; i < group.Objects.Count; i++)
                {
                    var child = group.Objects[i];
                    if (child != null && string.Equals(child.Name, name, StringComparison.OrdinalIgnoreCase))
                        return child;
                }
                return null;
            }
        }
    }
    /// <inheritdoc/>
    public override void Normalize()
    {
        EnsureUniqueObjectNames();
    }

    private void NormalizeCustomMaterialNames()
    {
        var materialMap = Level.Materials.GetCustomMaterialNameMap();
        if (materialMap.Count == 0)
            return;
        Stack<ObjectDefinition> objects = new();
        objects.Push(Root);
        while (objects.Count > 0)
        {
            var levelObject = objects.Pop();
            if (levelObject is Wall wall && materialMap.TryGetValue(wall.Material, out var wallMaterial))
                wall.Material = wallMaterial;
            if (levelObject is Wall wall2 && (wall2.Material == "Wood 2" || wall2.Material == "Wood 3"))
                wall2.Material = "Wood";
            if (levelObject is SetMaterialTriggerObjectDefinition setMaterial && materialMap.TryGetValue(setMaterial.Material, out var triggerMaterial))
                setMaterial.Material = triggerMaterial;
            if (levelObject is SetMaterialTriggerObjectDefinition setMaterial2 && (setMaterial2.Material == "Wood 2" || setMaterial2.Material == "Wood 3"))
                setMaterial2.Material = "Wood";
            if (levelObject is not Group group || group.Objects == null)
                continue;
            foreach (var child in group.Objects)
                if (child != null)
                    objects.Push(child);
        }
    }
    /// <summary>Ensures every group contains non-empty, case-insensitively unique child names.</summary>
    public void EnsureUniqueObjectNames()
    {
        foreach (var obj in Root.Recurse())
            if (obj is Group currentGroup)
                EnsureUniqueObjectNames(currentGroup);
    }

    /// <summary>Ensures a group's children have non-empty, case-insensitively unique names.</summary>
    /// <param name="group">The group to normalize.</param>
    public static void EnsureUniqueObjectNames(Group group)
    {
        var allocator = new ObjectNameAllocator(group.Objects.Count);

        foreach (var levelObject in group.Objects)
        {
            var name = levelObject.Name;

            if (string.IsNullOrWhiteSpace(name) || name == "Unknown")
                name = levelObject.GetDisplayName();

            levelObject.Name = allocator.Add(name);
        }
    }

    /// <summary>Allocates a unique name among a list of sibling objects.</summary>
    /// <param name="name">The preferred name.</param>
    /// <param name="siblings">The sibling objects whose names are reserved.</param>
    /// <param name="self">An optional object to exclude from the reserved names.</param>
    /// <returns>The original name or a numbered unique variant.</returns>
    public static string GetUniqueObjectName(
        string name,
        List<ObjectDefinition> siblings,
        ObjectDefinition self = null)
    {
        var allocator = new ObjectNameAllocator(siblings.Count);

        for (var i = 0; i < siblings.Count; i++)
        {
            var sibling = siblings[i];

            if (sibling != null && sibling != self)
                allocator.Reserve(sibling.Name);
        }

        return allocator.Add(name);
    }
    private sealed class ObjectNameAllocator
    {
        private readonly HashSet<string> UsedNames;
        private readonly Dictionary<string, int> NextNumbers;

        public ObjectNameAllocator(int capacity)
        {
            UsedNames = new HashSet<string>(
                capacity,
                StringComparer.OrdinalIgnoreCase
            );

            NextNumbers = new Dictionary<string, int>(
                StringComparer.OrdinalIgnoreCase
            );
        }

        public void Reserve(string name)
        {
            UsedNames.Add(name);
        }

        public string Add(string name)
        {
            if (UsedNames.Add(name))
                return name;

            SplitTrailingNumber(
                name,
                out var baseName,
                out var nextNumber
            );

            if (NextNumbers.TryGetValue(name, out var cachedNumber) &&
                cachedNumber > nextNumber)
            {
                nextNumber = cachedNumber;
            }

            while (true)
            {
                var candidate = $"{baseName} {nextNumber}";
                nextNumber++;

                if (!UsedNames.Add(candidate))
                    continue;

                NextNumbers[name] = nextNumber;
                return candidate;
            }
        }
    }

    private static void SplitTrailingNumber(
        string name,
        out string baseName,
        out int nextNumber)
    {
        var digitStart = name.Length;

        while (digitStart > 0 &&
               char.IsDigit(name[digitStart - 1]))
        {
            digitStart--;
        }

        if (digitStart == name.Length)
        {
            baseName = name;
            nextNumber = 2;
            return;
        }

        baseName = name.Substring(0, digitStart).TrimEnd();

        if (baseName.Length == 0 ||
            !int.TryParse(name.Substring(digitStart), out var number) ||
            number == int.MaxValue)
        {
            baseName = name;
            nextNumber = 2;
            return;
        }

        nextNumber = number + 1;
    }
}
