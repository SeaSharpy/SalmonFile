@echo off
setlocal EnableExtensions

set "SALMON_FILE_DIR=%~dp0"
for %%D in ("%SALMON_FILE_DIR%..") do set "REPOSITORY_ROOT=%%~fD"
set "OBJECTS_DIR=%SALMON_FILE_DIR%Objects"
set "SECTIONS_DIR=%SALMON_FILE_DIR%Sections"

if not exist "%REPOSITORY_ROOT%\Assets\Scripts\Salmon" (
    echo Client source directory not found beside SalmonFile.
    exit /b 1
)

if not exist "%OBJECTS_DIR%" mkdir "%OBJECTS_DIR%" || exit /b 1
if not exist "%SECTIONS_DIR%" mkdir "%SECTIONS_DIR%" || exit /b 1

copy /Y "%REPOSITORY_ROOT%\Assets\Scripts\Salmon\Levels\Enums.cs" "%SALMON_FILE_DIR%Enums.cs" >nul || exit /b 1
copy /Y "%REPOSITORY_ROOT%\Assets\Scripts\Salmon\Levels\Level.cs" "%SALMON_FILE_DIR%Level.cs" >nul || exit /b 1
copy /Y "%REPOSITORY_ROOT%\Assets\Scripts\Salmon\Levels\Sections\LevelSection.cs" "%SECTIONS_DIR%\LevelSection.cs" >nul || exit /b 1
copy /Y "%REPOSITORY_ROOT%\Assets\Scripts\Salmon\Levels\Objects\SettingsDefinition.cs" "%OBJECTS_DIR%\SettingsDefinition.cs" >nul || exit /b 1
copy /Y "%REPOSITORY_ROOT%\Assets\Scripts\Salmon\Levels\Objects\ObjectDefinitions.cs" "%OBJECTS_DIR%\ObjectDefinitions.cs" >nul || exit /b 1
copy /Y "%REPOSITORY_ROOT%\Assets\Scripts\Salmon\Levels\Objects\ObjectMetadata.cs" "%OBJECTS_DIR%\ObjectMetadata.cs" >nul || exit /b 1
copy /Y "%REPOSITORY_ROOT%\Assets\Scripts\Salmon\Levels\Sections\PreviewSection.cs" "%SECTIONS_DIR%\PreviewSection.cs" >nul || exit /b 1
copy /Y "%REPOSITORY_ROOT%\Assets\Scripts\Salmon\Levels\Sections\MaterialsSection.cs" "%SECTIONS_DIR%\MaterialsSection.cs" >nul || exit /b 1
copy /Y "%REPOSITORY_ROOT%\Assets\Scripts\Salmon\Levels\Sections\ObjectsSection.cs" "%SECTIONS_DIR%\ObjectsSection.cs" >nul || exit /b 1
copy /Y "%REPOSITORY_ROOT%\Assets\Scripts\Salmon\Levels\Sections\MetadataSection.cs" "%SECTIONS_DIR%\MetadataSection.cs" >nul || exit /b 1
copy /Y "%REPOSITORY_ROOT%\Assets\Scripts\Salmon\Levels\Sections\SettingsSection.cs" "%SECTIONS_DIR%\SettingsSection.cs" >nul || exit /b 1
copy /Y "%REPOSITORY_ROOT%\Assets\Scripts\Salmon\Dynamic\Generated\GeneratedDynamicSerializers.g.cs" "%SALMON_FILE_DIR%GeneratedDynamicSerializers.g.cs" >nul || exit /b 1
copy /Y "%REPOSITORY_ROOT%\Assets\Scripts\Salmon\Dynamic\Generated\GeneratedDynamicSerializers.cs" "%SALMON_FILE_DIR%GeneratedDynamicSerializers.cs" >nul || exit /b 1
copy /Y "%REPOSITORY_ROOT%\Assets\Scripts\Salmon\Dynamic\DynamicSerializer.cs" "%SALMON_FILE_DIR%DynamicSerializer.cs" >nul || exit /b 1
copy /Y "%REPOSITORY_ROOT%\Assets\Scripts\Salmon\Dynamic\DynamicInspectorAttributes.cs" "%SALMON_FILE_DIR%DynamicInspectorAttributes.cs" >nul || exit /b 1
copy /Y "%REPOSITORY_ROOT%\Assets\Scripts\Salmon\Dynamic\LimitedReadStream.cs" "%SALMON_FILE_DIR%LimitedReadStream.cs" >nul || exit /b 1
copy /Y "%REPOSITORY_ROOT%\Assets\Scripts\Salmon\Dynamic\TypeCache.cs" "%SALMON_FILE_DIR%TypeCache.cs" >nul || exit /b 1
copy /Y "%REPOSITORY_ROOT%\Assets\Scripts\Salmon\Snowflake.cs" "%SALMON_FILE_DIR%Snowflake.cs" >nul || exit /b 1
copy /Y "%REPOSITORY_ROOT%\Assets\Scripts\Salmon\OperandExpression.cs" "%SALMON_FILE_DIR%OperandExpression.cs" >nul || exit /b 1

echo Copied 19 level format sources into SalmonFile.
exit /b 0
