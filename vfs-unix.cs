using System;
using System.Collections.Generic;
using System.IO;
using Mono.Unix;
using Mono.Unix.Native;

namespace Mono.Vfs {

	public class UnixFileEntry : VirtualFileEntry {
		public UnixFileEntry (MountPoint mount, string parent, string name) : base (mount, parent, name)
		{
		}
	}

	public class UnixDirectoryEntry : VirtualDirectoryEntry {
		public UnixDirectoryEntry (MountPoint mount, string parent, string name) : base (mount, parent, name)
		{
		}

		protected override VfsError DoRename (string oldName, string newName)
		{
			throw new NotImplementedException ();
		}
		
		protected override VfsError DoDelete (string name)
		{
			throw new NotImplementedException ();
		}
		
		protected override VfsError DoPut (VirtualFileEntry source)
		{
			throw new NotImplementedException ();
		}
		
		protected override VfsError DoCopy (string source, string target)
		{
			throw new NotImplementedException ();
		}
		

		protected override VfsError DoMakeDirectory (string name)
		{
			throw new NotImplementedException ();
		}
		
		protected override VfsError DoRemoveDirectory (string name)
		{
			throw new NotImplementedException ();
		}
		

		protected override VfsError DoGetEntries (out VirtualEntry [] entries)
		{
			throw new NotImplementedException ();
		}
		
	}
	
	public class UnixMountPoint : MountPoint {
		static UnixMountPoint globalMountPoint = new UnixMountPoint ();
		
		public UnixMountPoint ()
		{
		}
		
		public static VirtualEntry UnixHandler (string path, string rest)
		{
			Stat stat;
			int r;
			
			do {
				r = Syscall.lstat (path, out stat);
			} while (r == -1 && Stdlib.GetLastError() == Errno.EINTR);
			if (r == -1)
				return null;

			string parent = UnixPath.GetDirectoryName (path);
			string name = UnixPath.GetFileName (path);
			
			if ((stat.st_mode & FilePermissions.S_IFMT) == FilePermissions.S_IFDIR)
				return new UnixDirectoryEntry (globalMountPoint, parent, name);
			else
				return new UnixFileEntry (globalMountPoint, parent, name);
		}
	}
}