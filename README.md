# Pattern finding script

The script takes two input arguments 
	1. The pattern.txt
	2. The Assembly Code to search in a txt file


Example command line execution of script.
	bash...
		$.\Program.exe pattern.txt Asembly_code.txt
	...

# Editing the pattern you wish to find
In the 'pattern.txt' file enter the pattern you wish to find.
The script is designed for use of Regular Expression.
*Note that '[' chars need to be used with an escape character like so '\[' *

The pattern files use special key words at the start of each line you wish to fine.

1. check: *code to find*
	this finds the line of code you wish to search for. It can be used in conjunction with 'check-next:'
	or on its own.

	check: add x3, x0, x1

2. check-next: *code to find*
	this command is used in conjunction with the 'check:' command. 

	check: add x3, x0, x1
	check-next: ldr     x1, \[x0\]   <-- note the escape character

#Regular Expression Compatible
The code is regular expression compatible as each line in the pattern is converted from string into a regex object.
This allows users to be able to find a variable starting with a specific character followed by any sequence of numbers or letters.

EXAMPLE:
	check: add x[0-9]+, x[0-9]+, x1

	This returns any line of code in this pattern --> "add x*number*, x*number*, x1"

Please see the 'Example Patterns Directory' which includes sample patterns used with the 'Asembly_code.txt' and their expected output.

#Variable usage in patterns

The script is also compatible with being able to assign a variable a value and use it later in the pattern.

using the folowing command (?<VAR>x1) captures the variable VAR with the value x1.
to use this later in a pattern (?<<VAR>>) is used.

EXAMPLE:
This pattern:
	check: add (?<REG1>r[0-9]+), (?<REG2>r[0-9]+), (?<REG3>r[0-9]+)
	check-next: sub (?<REG4>x[0-9]+), (?<REG5>x[0-9]+), (?<REG6>x[0-9]+)
	check-next: mul ?<<REG3>>, ?<<REG6>>, ?<<REG3>>
Will return as a match:
	add r1, r2, r3
	sub x1, x2, x3
	mul r3, x3, r3

#Comments in pattern
The pattern file is capable of ignoring comments through the use of '#' at the start of a line.
this will allow the user to identify a pattern file that they're using and wish to use again at a later date. By including the
reasons behind the pattern they wish to search for.


# Example Inputs and Outputs

#-- -- -- -- -- Input -- -- -- -- --
check: add     x1, x1, #16
check-next: add  x3, x0, x1
#-- -- -- -- -- -- -- -- -- -- -- --

#-- -- -- -- -- Output -- -- -- -- --
; Assembly listing for method System.Collections.Generic.Dictionary`2[ConfidenceLevel,ValueTuple`2][BenchmarkDotNet.Mathematics.ConfidenceLevel,System.ValueTuple`2[System.Int32,System.Int32]]:FindValue(int):byref:this

	G_M29703_IG09:
69962:	 add x0, x0, #16
69963:	 add x0, x22, x0
----------------------------------------------------------

	G_M29703_IG17:
70083:	 add x1, x1, #16
70084:	 add x1, x22, x1
----------------------------------------------------------
Matches Found = 24
time taken = 0.8005317 secs.
Pattern was found in:
	17 Methods. 
	23 groups.
#-- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- --
