# Files Directory

**`Files/`** | 🚧 Placeholder

Default filesystem image loaded into virtual filesystem at startup.

## Structure

```
Files/
└── Userspace/
    ├── bin/         # System binaries (init, shell)
    ├── programs/    # User programs
    ├── data/        # User data files
    └── lib/         # Reusable libraries
```

## Purpose

1. **Development** - Store files during development
2. **Compilation** - Embedded/packaged with app
3. **Runtime** - Loaded into virtual filesystem at startup

## File Mapping

```
Files/Userspace/programs/hello.cs → /programs/hello.cs
Files/Userspace/data/config.txt   → /data/config.txt
```

## Adding Files

Create files in `Files/Userspace/` respecting directory structure. Automatically available in virtual filesystem at runtime.

## Persistence

Currently **non-persistent** (in-memory only). Changes lost when program exits.

## See Also

[Filesystem](../System/Filesystem/FILESYSTEM.md) | [System Layer](../System/SYSTEM.md)
