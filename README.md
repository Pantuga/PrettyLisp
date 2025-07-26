# PrettyLisp

A little interpreted language I made to learn more about programming languages.
The syntax is lisp-based, with some teaks to make it more readable.

The code was made in C# and .NET.

## How to install

There aren't any binaries in the repository yet, so you might have to compile it yourself.
The solution file and the `.csproj` file are both avaliable in the repository.

## How to use the program

You can execute the program in the command line with the path to code file as the first argument to run it:
```
.\PrettyLisp.exe FileName.ptl
```
I have used the `.ptl` extesion in my examples, but you can use any other file extension (e.g. `.txt`)

If you don't want to use the command line, you can simply select the code file, click `Open With` and select the executable.

Alternatively, you can open the REPL by executing the program without any arguments.
if you want to open a file in the REPL, type `file` and then the path of the file:
```
>>file
FileName.ptl
```
If you do this, you might want to reset the interpreter before you run another code file, as all the variables stay stored in the REPL.
You can do this by typing `reset`.
