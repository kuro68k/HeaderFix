# HeaderFix

Bulk edit headers in C/C++ files. Headers are the top C style comment starting on the first line of a source code file. Create a header file with a single C style comment starting on the first line:

```
/*
 * $filename
 * (C) Me $a0
 * Licence: $a1
 *\
```

The comment must be closed. $ is used as an escape character for substitutions:

```
$$          $ sign
$filename   current file's name
$a0 - $a9   arbitrary strings supplied on the command line
```

Then involke HeaderFix as follows:

```
HeaderFix [options] -h <header file> <input pattern>

-h <header.h>   Header file, mandatory
-c				Process C/C++/C# files (.c, .cpp, .h, .cs)
-r              Recurse into subdirectories
-q              Quiet, only print errors
-v              Verbose, print status of each file
-d              Dry run, combine with -v to see what will be changed
-p              Preserve file written timestamps
-a0 to a9       Arbitrary substiution strings
```

`input pattern` can be a single file, a directory, or a pattern like `project\*.c`. Multiple inputs can be supplied, separated by a semicolon. Examples:

`HeaderFix -h header.h c:\code\project`<br>
*Process all files in c:\code\project*<br>
`HeaderFix -h header.h c:\code\project\*.h`<br>
*Process headers in c:\code\project*<br>
`HeaderFix -h header.h -r -c c:\code\project`<br>
*Process all .c, .cpp, .h and .cs files in c:\code\project and all subdirectories*

Arbitrary strings example:

`HeaderFix -h header.h -v -p -a0 "2017" -a1 "GPL v3" project\*.c;project\*.h`

All `.c` and `.h` files in `project` will have the comment starting on the first line (the header comment) replaced with:

```
/*
 * filename.c
 * (C) Me 2017
 * Licence: GPL v3
 *\
```
