# Filesystem

**`System/Filesystem/Filesystem.cs`** | `BlackBox.System.Filesystem` | 🚧 Placeholder

Virtual filesystem - sandboxed, no direct host access.

## Planned API

```csharp
// File operations
string Read(string path)
byte[] ReadBytes(string path)
Write(string path, string content)
WriteBytes(string path, byte[] data)
Append(string path, string content)
bool Exists(string path)
Delete(string path)
Copy(string source, string dest)
Move(string source, string dest)

// Directory operations
IEnumerable<string> List(string path)
IEnumerable<FileInfo> ListDetailed(string path)
CreateDirectory(string path)
bool DirectoryExists(string path)
DeleteDirectory(string path, bool recursive = false)
string CurrentDirectory { get; set; }

// Path operations
string Combine(params string[] paths)
string GetAbsolutePath(string path)
string GetParent(string path)
string GetFileName(string path)
string GetExtension(string path)

// File information
long GetSize(string path)
DateTime GetCreatedTime(string path)
DateTime GetModifiedTime(string path)
FileInfo GetInfo(string path)
```

## Planned Types

```csharp
class FileInfo {
    string Name; string Path; long Size;
    FileType Type; DateTime Created; DateTime Modified;
    FileAttributes Attributes;
}

enum FileType { File, Directory, Link }

[Flags] enum FileAttributes {
    None = 0, ReadOnly = 1, Hidden = 2, System = 4, Archive = 8
}
```

## Virtual Structure

```
/ (root)
├── programs/    # User programs
├── data/        # User data
└── system/      # System files
```

Initialized from `Files/Userspace/` at startup. Unix-style paths only.

## See Also

[IO](../IO.md) | [Files](../../Files/FILES.md)
