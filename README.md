# DataHardLinkHelper
Save a ton of disk space by using hard links.

This tool will collect unique files and add them to a folder along with renaming them to their MD5 hash and making an instance.xml that contains their original file names and directories.
Then you can hard link all files from an instance.xml to your desired target directory, along with their original filenames and folder locations.

Other features include: copying files from an instance, deleting unused DB files.
