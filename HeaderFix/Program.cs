using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.InteropServices;
using Keio.Utils;

namespace HeaderFix
{
	class Program
	{
		static bool quiet = false;
		static bool verbose = false;
		static bool dry_run = false;
		static bool preserve_timestamps = false;

		static int Main(string[] args)
		{
			string input = string.Empty;
			string headerFile = string.Empty;
			bool recurseDirs = false;
			string[] arbs = new string[10];

			CmdArgs argProcessor = new CmdArgs() {
				{ new CmdArgument("h,header", ArgType.String, required: true,
					help: "Header file",
					parameter_help: "header.h",
					assign: (dynamic d) => { headerFile = (string)d; }) },

				{ new CmdArgument("r,recurse", ArgType.Flag, required: false,
					help: "Recurse into subdirectories",
					assign: (dynamic d) => { recurseDirs = (bool)d; }) },
				
				{ new CmdArgument("q,quiet", ArgType.Flag, required: false,
					help: "Surpress output",
					assign: (dynamic d) => { quiet = true; }) },

				{ new CmdArgument("v,verbose", ArgType.Flag, required: false,
					help: "More output",
					assign: (dynamic d) => { verbose = true; }) },

				{ new CmdArgument("d,dry", ArgType.Flag, required: false,
					help: "Dry run (no files changed)",
					assign: (dynamic d) => { dry_run = true; }) },

				{ new CmdArgument("p,preserve", ArgType.Flag, required: false,
					help: "Preserve modified file timestamps",
					assign: (dynamic d) => { preserve_timestamps = true; }) },

				{ new CmdArgument("input file/dir", ArgType.String, required: true,
					anonymous: true,
					help: "File or directory to process",
					parameter_help: "input file/dir",
					assign: (dynamic d) => { input = (string)d; }) },

				{ new CmdArgument("a0", ArgType.String, required: false,
									help: "Arbitrary substitution string 0",
									assign: (dynamic d) => { arbs[0] = (string)d; }) },
				{ new CmdArgument("a1", ArgType.String, required: false,
									help: "Arbitrary substitution string 1",
									assign: (dynamic d) => { arbs[1] = (string)d; }) },
				{ new CmdArgument("a2", ArgType.String, required: false,
									help: "Arbitrary substitution string 2",
									assign: (dynamic d) => { arbs[2] = (string)d; }) },
				{ new CmdArgument("a3", ArgType.String, required: false,
									help: "Arbitrary substitution string 3",
									assign: (dynamic d) => { arbs[3] = (string)d; }) },
				{ new CmdArgument("a4", ArgType.String, required: false,
									help: "Arbitrary substitution string 4",
									assign: (dynamic d) => { arbs[4] = (string)d; }) },
				{ new CmdArgument("a5", ArgType.String, required: false,
									help: "Arbitrary substitution string 5",
									assign: (dynamic d) => { arbs[5] = (string)d; }) },
				{ new CmdArgument("a6", ArgType.String, required: false,
									help: "Arbitrary substitution string 6",
									assign: (dynamic d) => { arbs[6] = (string)d; }) },
				{ new CmdArgument("a7", ArgType.String, required: false,
									help: "Arbitrary substitution string 7",
									assign: (dynamic d) => { arbs[7] = (string)d; }) },
				{ new CmdArgument("a8", ArgType.String, required: false,
									help: "Arbitrary substitution string 8",
									assign: (dynamic d) => { arbs[8] = (string)d; }) },
				{ new CmdArgument("a9", ArgType.String, required: false,
									help: "Arbitrary substitution string 9",
									assign: (dynamic d) => { arbs[9] = (string)d; }) },
			};

			string[] remainder;
			if (!argProcessor.TryParse(args, out remainder))
			{
				Console.WriteLine();
				argProcessor.PrintHelp();
				return 1;
			}

			byte[] header = ReadHeader(headerFile);
			if (header == null)
			{
				Console.WriteLine("Unable to read header");
				return 1;
			}

			return ProcessAllFiles(input, header, arbs, recurseDirs);
		}

		private static int ProcessAllFiles(string inputs, byte[] header, string[] arbs, bool recurseDirs)
		{
			int successes = 0;
			int failures = 0;
			int unchanged = 0;
			SearchOption so;
			so = recurseDirs ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

			string[] input_list = inputs.Split(';');

			foreach (string input in input_list)
			{
				// get full path and file name/pattern
				string pattern = Path.GetFileName(input);
				string relative_path = input.Substring(0, input.Length - pattern.Length);
				string absolute_path = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), relative_path));
				string[] file_list = Directory.GetFiles(absolute_path, pattern, so);

				if (file_list.Length == 0)
				{
					if (!quiet)
						Console.WriteLine("No matches for \"" + input + "\".");
					continue;
				}

				for (int i = 0; i < file_list.Length; i++)
				{
					bool changed = false;
					bool res = ProcessFile(file_list[i], header, arbs, out changed);
					if (res && changed) successes++;
					else if (res) unchanged++;
					else failures++;
				}
			}

			if (!quiet)
			{
				Console.WriteLine("Changed:\t" + successes.ToString());
				Console.WriteLine("Unchanged:\t" + unchanged.ToString());
				Console.WriteLine("Failures:\t" + failures.ToString());
			}

			return 0;
		}

		private static string GetRelativePath(string file)
		{
			string current_path = Directory.GetCurrentDirectory();
			if (file.StartsWith(current_path))
				return file.Substring(current_path.Length + 1);
			return file;
		}

		private static bool ProcessFile(string filename, byte[] header, string[] arbs, out bool changed)
		{
			changed = false;
			string rel = GetRelativePath(filename);

			if (!File.Exists(filename))
			{
				if (!quiet)
					Console.WriteLine(rel + ": File not found");
				return false;
			}
			
			byte[] src;
			try
			{
				src = File.ReadAllBytes(filename);
			}
			catch (Exception e)
			{
				if (!quiet)
					Console.WriteLine(e.Message);
				return false;
			}

			// sanity checks
			if (src.Count() < 4)
			{
				if (!quiet)
					Console.WriteLine(rel + ": File too short");
				return false;
			}

			// look for old header
			int src_header_end;
			if (!FindComment(src, out src_header_end))
			{
				if (!quiet)
					Console.WriteLine(rel + ": Header not found");
				return false;
			}

			// process header for this file
			byte[] processed_header = new byte[header.Length];
			processed_header = ProcessSubstitutions(header, filename, arbs);

			int new_length = processed_header.Length + src.Length - src_header_end;
			byte[] new_file = new byte[new_length];
			Array.Copy(processed_header, new_file, processed_header.Length);
			Array.Copy(src, src_header_end, new_file, processed_header.Length, src.Length - src_header_end);

			changed = !ByteArrayCompare(src, new_file);
			if (changed && !dry_run)
			{
				DateTime dt = File.GetLastWriteTime(filename);
				File.WriteAllBytes(filename, new_file);
				if (preserve_timestamps)
					File.SetLastWriteTime(filename, dt);
			}

			if (verbose)
			{
				string padded = PadFilename(rel, 68);
				Console.Write(padded + " ");
				if (changed)
				{
					Console.ForegroundColor = ConsoleColor.DarkGreen;
					Console.WriteLine("OK");
					Console.ResetColor();
				}
				else
				{
					Console.ForegroundColor = ConsoleColor.DarkCyan;
					Console.WriteLine("UNCHANGED");
					Console.ResetColor();
				}
			}

			return true;
		}

		static string PadFilename(string filename, int length)
		{
			if (filename.Length == length)
				return filename;
			
			while (filename.Length > length)
			{
				int p = filename.IndexOf(Path.VolumeSeparatorChar);
				if (p == -1)
					p = filename.IndexOf(Path.DirectorySeparatorChar);
				if (p == -1)
					p = filename.IndexOf(Path.AltDirectorySeparatorChar);
				if ((p != -1) && (filename.Length > p + 1))
					filename = filename.Substring(p + 1);
				else
					break;
			}

			if (filename.Length > length)
				filename = "..." + filename.Substring(filename.Length - length + 3);

			if (filename.Length < length)
				filename = filename.PadRight(length);

			return filename;
		}

		[DllImport("msvcrt.dll", CallingConvention = CallingConvention.Cdecl)]
		static extern int memcmp(byte[] b1, byte[] b2, long count);

		private static bool ByteArrayCompare(byte[] b1, byte[] b2)
		{
			return b1.Length == b2.Length && memcmp(b1, b2, b1.Length) == 0;
		}

		private static byte[] ReadHeader(string filename)
		{
			if (!File.Exists(filename))
			{
				if (!quiet)
					Console.WriteLine("File not found (" + filename + ")");
				return null;
			}

			// read from source file
			byte[] header;
			try
			{
				header = File.ReadAllBytes(filename);
			}
			catch (Exception e)
			{
				if (!quiet)
					Console.WriteLine(e.Message);
				return null;
			}
			
			// sanity check
			int len = header.Length;
			if (len == 0)
			{
				if (!quiet)
					Console.WriteLine("Header file empty (" + filename + ")");
				return null;
			}

			// trim trailing newlines
			len--;
			while (len > 0)
			{
				if ((header[len] != '\r') && (header[len] != '\n'))
					break;
				len--;
			}
			if (len != header.Length - 1)	// something was trimmed
			{
				byte[] trimmed = new byte[len + 1];
				Array.Copy(header, trimmed, len + 1);
				header = trimmed;;
			}

			// sanity check
			if (header.Length == 0)
			{
				if (!quiet)
					Console.WriteLine("Header file empty after trimming (" + filename + ")");
				return null;
			}

			int end;
			if (FindComment(header, out end))
			{
				if (!quiet)
					Console.WriteLine("Header does not contain comment /* ... */ (" + filename + ")");
				return null;
			}
			
			return header;
		}

		// find a C style comment /* ... */ at the start of the buffer
		// end points to the character after the closing / character
		private static bool FindComment(byte[] buffer, out int end)
		{
			end = -1;

			if (buffer.Length < 4)	// too small to contain a comment
				return false;

			// look for start of header
			if ((buffer[0] != '/') || (buffer[1] != '*'))
					return false;

			// look for end of header
			int i = 2;
			while (i < buffer.Length - 2)
			{
				if ((buffer[i] == '*') && (buffer[i + 1] == '/'))
				{
					end = i + 2;
					break;
				}
				i++;
			}
			if (end == -1)
				return false;

			return true;
		}

		// replace $identifier values in byte stream
		private static byte[] ProcessSubstitutions(byte[] buffer, string filename, string[] arb)
		{
			int i;
			MemoryStream ms = new MemoryStream();
			filename = Path.GetFileName(filename);
			byte[] filename_array = Encoding.UTF8.GetBytes(filename);
			byte[][] arb_array = new byte[10][];
			if (arb != null)
			{
				for (i = 0; i < arb_array.Length - 1; i++)
				{
					if (i > arb.Length - 1)
						break;
					if (arb[i] != null)
						arb_array[i] = Encoding.UTF8.GetBytes(arb[i]);
				}
			}

			i = 0;
			while(i < buffer.Length)
			{
				if ((char)buffer[i] == '$')
				{
					string id = "";
					i++;
					while (i < buffer.Length)
					{
						id += (char)buffer[i];

						bool handled = true;
						switch(id)
						{
							case "filename":
								ms.Write(filename_array, 0, filename_array.Length);
								break;

							case "0":
							case "1":
							case "2":
							case "3":
							case "4":
							case "5":
							case "6":
							case "7":
							case "8":
							case "9":
								int idx = int.Parse(id);
								if (idx > arb_array.Length - 1)
								{
									if (!quiet)
										Console.WriteLine("Unknown arb parameter (" + idx.ToString() + ")");
									return null;
								}
								else
								{
									if (arb_array[idx] != null)
										ms.Write(arb_array[idx], 0, arb_array[idx].Length);
								}
								break;

							case "$":
								ms.WriteByte((byte)'$');
								break;

							case "tab":
								ms.WriteByte((byte)'\t');
								break;

							default:
								handled = false;
								break;
						}

						if (handled)
							break;

						i++;
					}
				}
				else
					ms.WriteByte(buffer[i]);
				
				i++;
			}

			return ms.ToArray();
		}

		// skips over characters in skip_chars, returns -1 if end of buffer reached
		private static int SkipChars(byte[] buffer, string skip_chars)
		{
			int i = 0;
			while (i < buffer.Length - 1)
			{
				char c = (char)buffer[i];
				if (!skip_chars.Contains(c))
					return i;
				i++;
			}
			return -1;
		}
	}
}
