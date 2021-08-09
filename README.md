# verify-swagger-and-codemethods

Running this code generates a series of .txt files in a csv format. They compare the methods from the Swagger file to the methods from the code generation.<br />
The code outputs 4 different .txt files:
*	comparisonLines.txt – in the left side, all methods from Swagger, on the right side, all methods from the code
*	matchinglines.txt – a list of method names that match between the Swagger and the code
*	mismatchedLines.txt – in the left side, all methods from Swagger that don’t yet have a match, on the right side, all methods from the code that aren’t yet matched
*	secondPass.txt – the entries from the mismatchedLines.txt have been modified to see if any matches can be made. The remainder of the mismatched lines are outputted in this file.

To run this code:
1. Modify line 17 in VerifyMethods.csproj to point to the .csproj of Azure.ResourceManager.Sample on your computer.
2. Modify line 25 in VerifyMethodsTests.cs to point at the Swagger file of Azure.ResourceManager.Sample. 
3. Modify lines 100, 149, 229, and 327 to point at the directory the txt should be outputted to. 
4. Run VerifyMethods to generate the 4 files. <br />

Necessary changes have been marked with a TODO tag.
