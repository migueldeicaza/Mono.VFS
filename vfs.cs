//
// Tasks:
//   * Ability to list a directory
//   * Ability to perform operations on a directory:
//       * Rename items
//       * Delete items
//       * GET
//       * PUT
//       * COPY (local to the "mount point")
//       * Obtain list of items.
//       * CanStream (to implement viewers that do not require a GET)
//   * Get a Directory "handle" or a File "handle" from a URL
//   
// Internals:
//   * Keep a cache of "mount points", to avoid reloading/re-log-in
//       * ie: sshfs://miguel@foobar/dir1
//       * ie: ftpfs://foobar/pub/dir1/dir2/README
//   * Mount points are of a given file system type
//   * Directories are parsed as local parameters to a Mount Point
//   * Files are relative to a Directory
//
using System;
using System.Collections.Generic;
using System.IO;

//
// zip:/tmp/mcs.zip#mcs/Makefile
// ftp://miguel@gnome.org/pub/README
// zip:ftp://miguel@gnome.org/pub/mcs.zip#mcs/Makefile
// /tmp/
// /tmp/file.c


namespace Mono.Vfs {
	public delegate VirtualEntry VfsHandler (string path, string relativeData);

	public enum VfsError {
		None,
		Permission,
		FileNotFound,
		IOError,
		AccessDenied,
		Busy,
		NotADirectory,
		NoSpaceLeft,
		ReadOnly,
		FilenameTooLong,
		DirectoryNotEmpty,
		Other
	}
	
	public class VfsHandlers {
		static Dictionary<string, VfsHandler> handlers = new Dictionary<string,VfsHandler> ();
		
		static VfsHandlers ()
		{
			//handlers.Add ("fake://", FakeHandler);
		}

		// Split the data into the base url for the handler to operate on, and relative
		// information required.
		// For example:
		//    zip://demo.zip       => (demo.zip, )
		//    zip:/demo.zip/docs  => (demo.zip, docs)
		//    zip://targz://http://www.go-mono.com/demo.tar.gz#mcs/file/assemblies.zip#mscorlib.dll
		//       => (targz://http://www.go-mono.com/demo.tar.gz#mcs/file/assemblies.zip, mscorlib.dll)
		//
		static VirtualEntry Activate (string urlPath, string prefix, VfsHandler handler)
		{
			string path = urlPath.Substring (prefix.Length);
			int lastHash = path.LastIndexOf ('#');
			string relativeData = lastHash == -1 ? "" : path.Substring (lastHash+1);
			if (lastHash != -1)
				path = path.Substring (0, lastHash);

			return handler (path, relativeData);
		}
		
		public static VirtualEntry FromPath (string path)
		{
			if (path == null)
				throw new ArgumentNullException ("path");

			foreach (var de in handlers){
				if (!path.StartsWith (de.Key))
					continue;
				return Activate (path, de.Key, de.Value);
			}
			return UnixMountPoint.UnixHandler (path, null);
		}

		public static bool IsAbsolute (string path)
		{
			if (path == null)
				throw new ArgumentNullException ("path");

			foreach (var de in handlers){
				if (!path.StartsWith (de.Key))
					continue;
				return true;
			}
			return false;
		}
	}
	
	public class MountPoint {
		public bool IsReadOnly;
	}

	public class VirtualEntry {
		public MountPoint MountPoint { private set; get; }
		public string Parent { private set; get; }
		public string Name { private set; get; }
		
		public VirtualEntry (MountPoint mount, string parent, string name)
		{
			MountPoint = mount;
			Parent = parent;
			Name = name;
		}
		
		public static VirtualEntry FromUrl (string url)
		{
			if (url == null)
				throw new ArgumentNullException ("url");

			return VfsHandlers.FromPath (url);
		}
	}

	public class VirtualFileEntry : VirtualEntry {
		public VirtualFileEntry (MountPoint mount, string parent, string name) : base (mount, parent, name)
		{
		}
	}
	
	public abstract class VirtualDirectoryEntry : VirtualEntry {
		public VirtualDirectoryEntry (MountPoint mount, string parent, string name) : base (mount, parent, name)
		{
		}

		protected abstract VfsError DoRename (string oldName, string newName);
		public VfsError Rename (string oldName, string newName)
		{
			if (oldName == null)
				throw new ArgumentNullException ("oldName");
			if (newName == null)
				throw new ArgumentNullException ("newName");
			if (MountPoint.IsReadOnly)
				return VfsError.ReadOnly;
			
			return DoRename (oldName, newName);
		}
		

		protected abstract VfsError DoDelete (string name);
		public VfsError Delete (string name)
		{
			if (name == null)
				throw new ArgumentNullException ("name");
			if (MountPoint.IsReadOnly)
				return VfsError.ReadOnly;
			
			return DoDelete (name);
		}
		
		protected abstract VfsError DoPut (VirtualFileEntry source);
		public VfsError Put (VirtualFileEntry source)
		{
			if (source == null)
				throw new ArgumentNullException ("source");
			if (MountPoint.IsReadOnly)
				return VfsError.ReadOnly;
			
			return DoPut (source);
		}

		//
		// This one needs to work across mountpoints
		//
		protected abstract VfsError DoCopy (string source, string target);
		public VfsError Copy (string source, string target)
		{
			if (source == null)
				throw new ArgumentNullException ("source");
			if (target == null)
				throw new ArgumentNullException ("target");
			
			return DoCopy (source, target);
		}
		
		protected abstract VfsError DoMakeDirectory (string name);
		public VfsError MakeDirectory (string name)
		{
			if (name == null)
				throw new ArgumentNullException ("name");
			return DoMakeDirectory (name);
		}
		
		protected abstract VfsError DoRemoveDirectory (string name);
		public VfsError RemoveDirectory (string name)
		{
			if (name == null)
				throw new ArgumentNullException ("name");
			return DoRemoveDirectory (name);
		}
		
		protected abstract VfsError DoGetEntries (out VirtualEntry [] entries);
		public VfsError GetEntries (out VirtualEntry [] entries)
		{
			return DoGetEntries (out entries);
		}

		public VfsError Move (string source, string target)
		{
			if (source == null)
				throw new ArgumentNullException ("source");
			if (target == null)
				throw new ArgumentNullException ("target");

			if (VfsHandlers.IsAbsolute (source) || VfsHandlers.IsAbsolute (target)){
				var sentry = VirtualEntry.FromUrl (source);
				var tentry = VirtualEntry.FromUrl (target);

				if (sentry.MountPoint != tentry.MountPoint){
					
				}
			}
			return VfsError.None;
		}
	}
}