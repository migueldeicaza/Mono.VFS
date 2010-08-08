vfs.dll: vfs.cs vfs-unix.cs
	gmcs -target:library vfs.cs vfs-unix.cs -r:Mono.Posix