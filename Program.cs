using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
#pragma warning disable CS8603 // Possible null reference return.

public class pattern_finder
{
    //dictionary uses the captured variable name in the first string
    //and assigns its content in the second string
    public bool testOngoing = false;

    public Dictionary<string, string> capturedVariables = new Dictionary<string, string>();

    public StringBuilder buildReturnStr = new StringBuilder();

    public List<string> found = new List<string>();
    public List<string> tempFoundCode = new List<string>();
    public List<foundCode> foundCodeSnippets = new List<foundCode>();

    public List<assemblyMethod> assemblyMethods = new List<assemblyMethod>();
    List<Regex> inPattern = new List<Regex>();
    string[] pattern = new string[0];
    public string rex = @"\s*([0-9A-F]{8})\s+(\w+)\s+(.*)";
    public int counter = 0;
    public int methodsFound = 0;
    public int groupsFound = 0;

    public static void Main(string[] args)
    {
        pattern_finder p = new pattern_finder();
        try
        {
            if (args.Length<2)
            {
                throw new notEnoughArgumentsException();
            }
            if (!File.Exists(args[0]))
            {
                throw new fileDoesntExistException(String.Format("File "+args[0]+" in Path doesnt exist"));
            }
            if (!File.Exists(args[1]) )
            {
                throw new fileDoesntExistException(String.Format("File " + args[1] + " in Path doesnt exist"));
            }
            string pattern = args[0];
            string assembly = args[1];
            patternReturnInfo patternReturnInfo = new patternReturnInfo();
            patternReturnInfo = p.heartBeat(pattern, assembly);
            if (patternReturnInfo != null)
            {
                p.printFoundPatterns(patternReturnInfo);//only prints to screen when running
            }
        }
        catch (notEnoughArgumentsException ex)
        {
            Console.WriteLine(ex.Message);
        }
    }

        public void testInProgress()
    {
        testOngoing = true;
    }
    public StringBuilder heartbeatReturnString(patternReturnInfo fndInfo)
    {
        if (fndInfo != null)
        {
            List<foundCode> fndSnps = fndInfo.code;


            foreach (foundCode fc in fndSnps)
            {
                if(!string.IsNullOrEmpty(fc.method))
                {
                    methodsFound++;
                    buildReturnStr.Append(fc.method);
                }
                if (!string.IsNullOrEmpty(fc.groupName))
                {
                    groupsFound++;
                    buildReturnStr.Append(fc.groupName);
                }
                foreach (string line in fc.code)
                {
                    buildReturnStr.Append(line);
                }
            }
            fndInfo.otherInfo.Add("\t" + methodsFound + " Methods. \t" + groupsFound + " groups.");
            foreach (string line in fndInfo.otherInfo)
            {
                buildReturnStr.Append(line);
            }
            string ret = buildReturnStr.ToString();
            return buildReturnStr;
        }
        else
        {
            return buildReturnStr;
        }
    }

    public patternReturnInfo heartBeat(string pattern, string assembley)
    {
        pattern_finder p = new pattern_finder();

        p.pattern = System.IO.File.ReadAllLines(pattern);
        string[] assembly = System.IO.File.ReadAllLines(assembley);

        //Pattern file is empty try/ catch
        try
        {
            p.patternCleaner();//removes extra spaces
        }
        //error catch for empty Pattern file
        catch (EmtpyPatternFileException ex)
        {
            Console.WriteLine(ex.Message);
            Environment.Exit(1);
        }
        p.inputToRegex(p.pattern); // converts to regex (combine in above method in future)
        try
        {
            p.removeComments(assembly); //removes comments and assigns line number of code from original file
        }
        catch (EmptyAssemblyFileException ex)
        {
            Console.WriteLine(ex.Message);
            Environment.Exit(1);
        }
        try
        {
            p.patternChecker();//main function checking the pattern against each line of code in the assembly file
        }
        catch (InvalidPatternException ex)
        {
            Console.WriteLine(ex.Message);
            Environment.Exit(2);
        }
        
        //if a match is found prints output,
        if (p.counter > 0)
        {
            patternReturnInfo patRetInf = new patternReturnInfo();
            patRetInf.code = p.foundCodeSnippets;

            patRetInf.otherInfo.Add("Matches Found = " + p.counter);
            patRetInf.otherInfo.Add("Pattern was found in:");

            return patRetInf;
        }

        //if no matches found warning
        else
        {
            if (testOngoing == true) {

                Console.WriteLine("\n\n" + p.counter + " Matches Found \n\nCheck your Pattern file for correct input");
            }
            buildReturnStr.Append(p.counter + " Matches Found Check your Pattern file for correct input");

            return null;

        }
    }
    public void patternChecker()
    {
        
        bool methodAdded = false;
        bool groupChanged = false;
        //gets set to true when 'check' can no longer be used
        bool invalidPattern = false;
        int currPatternLine = 0;

        int nextLine = 0;

        int tempLine = 0;
        int tempGroup = 0;
        int tempMethod = 0;
        assemblyCode tempAc = new assemblyCode();
        assemblyMethod tempAm = new assemblyMethod();

        for(int method = 0; method < assemblyMethods.Count;)// for each method
        {
            assemblyMethod testAm = assemblyMethods[method];
            methodAdded = false;
            for(int group = 0; group < testAm.groups.Count;) //for each group in every method
            {
                groupChanged = false;
                assemblyCode testAc = testAm.groups[group];

                for (int currLine = 0; currLine < testAc.code.Count;) //each line of code in a group
                {
                    string currPat = inPattern[currPatternLine].ToString(); //sets currently pattern from input string
                    string comp = Regex.Replace(currPat, @"^[^\s]+\s*", ""); //changes input string to regular expression removing the search definition
                    string curCode = assemblyMethods[method].groups[group].code[currLine]; //the current line of code being checked
                    nextLine = currLine + 1;
                    if (comp.Length<1)
                    {
                        throw new EmtpyPatternFileException();
                    }
                    if (currPat.Contains("?<<")) //used for named parameters (ie assign a value to a parameter)
                    {
                        Regex extractorRegex = new Regex(@"\?<<(\w+)>>"); //removes the identifying syntax
                        MatchCollection extractedMatch = extractorRegex.Matches(currPat);
                        foreach (Match match in extractedMatch)
                        {
                            currPat = currPat.Replace(match.Captures[0].Value, capturedVariables[match.Groups[1].Value]);
                        }
                        comp = Regex.Replace(currPat, @"^[^\s]+\s*", "");
                    }

                    if (currPat.StartsWith("check-next:") && checkNext(comp, method, group, currLine))
                    {
                        //invalidPattern = true;
                        string toAdd = assemblyMethods[method].groups[group].lineNum[currLine]+":\t "+ curCode;
                        tempFoundCode.Add(toAdd);
                        currPatternLine++;
                    }

                    else if (currPat.StartsWith("check:") && check(comp, method, group, currLine) && currPatternLine == 0)
                    {
                        //stores found patter temp until full match found or cancelled
                        tempLine = currLine + 1;
                        tempGroup = group;
                        tempMethod = method;
                        tempAc = testAc;
                        tempAm = testAm;
                        string toAdd = assemblyMethods[method].groups[group].lineNum[currLine] + ":\t " + curCode;
                        tempFoundCode.Add(toAdd);

                        currPatternLine++;
                    }
                    else if (currPat.StartsWith("check:") && currPatternLine > 0)
                    {
                        if (invalidPattern == true)
                        {
                            throw new InvalidPatternException();
                        }
                        else if (check(comp, method, group, currLine))
                        {
                            string toAdd = assemblyMethods[method].groups[group].lineNum[currLine] + ":\t " + curCode;
                            tempFoundCode.Add(toAdd);
                            currLine = nextLine;
                            currPatternLine++;
                        }
                        else
                        {
                            currLine = nextLine;
                            continue;
                        }
                    }
                    else if (currPatternLine == 0)//iterates to next line to be checked
                    {
                        currLine = nextLine;
                        continue;
                    }
                    else
                    {
                        tempFoundCode.Clear();
                        capturedVariables.Clear();
                        invalidPattern = false;
                        nextLine = tempLine;
                        group = tempGroup;
                        method = tempMethod;
                        testAc = tempAc;
                        testAm = tempAm;

                        currPatternLine = 0;
                    
                    }
                    if (currPatternLine >= inPattern.Count) //successful pattern found, clear temp objects
                    {
                        invalidPattern = false;
                        counter++;
                        //add in found patterns data
                        tempFoundCode.Add("----------------------------------------------------------");
                        foundCode newFoundPattern = new foundCode();
                        if (methodAdded == false)
                        {
                            methodAdded = true;
                            string FoundPatterns = assemblyMethods[method].functionName;
                            //found.Add(FoundPatterns);
                            newFoundPattern.method = FoundPatterns;
                        }
                        if (groupChanged == false)
                        {
                            groupChanged = true;
                            string groupToAdd = "\t" + assemblyMethods[method].groups[group].name;
                            newFoundPattern.groupName = groupToAdd;
                        }
                        addCodeToList(newFoundPattern);
                        currPatternLine = 0;
                        tempFoundCode.Clear();
                        capturedVariables.Clear();
                    }
                    currLine = nextLine;
                }
                group++;
            }
            method++;
            tempFoundCode.Clear();
            currPatternLine = 0;
        }
    }
    public void addCodeToList(foundCode newFoundCode)
    {
        foreach (string line in tempFoundCode)
        {
            newFoundCode.code.Add(line);
        }
        foundCodeSnippets.Add(newFoundCode);
    }
    public void printFoundPatterns(patternReturnInfo fndInfo)
    {
        List<foundCode> fndSnps = fndInfo.code;

        foreach (foundCode fc in fndSnps)
        {
            if(!string.IsNullOrEmpty(fc.method))
            {
                Console.WriteLine(fc.method);
                methodsFound++;

                buildReturnStr.Append(fc.method);
            }
            if(!string.IsNullOrEmpty(fc.groupName))
            {
                Console.WriteLine(fc.groupName);
                groupsFound++;

                buildReturnStr.Append(fc.groupName);
            }
            foreach(string line in fc.code)
            {
                Console.WriteLine("\t\t"+line);

                buildReturnStr.Append(line);
            }
        }
        fndInfo.otherInfo.Add("\t" + methodsFound + " Methods. \t" + groupsFound + " groups.");
        foreach (string line in fndInfo.otherInfo)
        {
            Console.WriteLine(line);
            buildReturnStr.Append(line);
        }
        //saves as a local text file to be used for testing purposes
        //StreamWriter file = new StreamWriter(@"C:\Users\2cath\OneDrive\Documents\College\Fourth_Year\OSSE\Ongoing_Project\assembly_patterns\Example_Patterns\pattern_0005_correct.txt");
        //file.Write(buildReturnStr.ToString());
        //file.Close();
    }
    public void inputToRegex(string[] input)
    {
        foreach (string inn in input)
        {
            string cus = Regex.Replace(inn, "\\s+", " ");
            Regex ret = new Regex(cus);
            inPattern.Add(ret);
        }
    }
    public void patternCleaner()
    {
        List<string> temp = new List<string>();
        int x = 0;
        for (int i = 0; i < pattern.Length; i++)
        {
            if (pattern[i].StartsWith("#"))
            {
                continue;
            }
            else
            {
                temp.Add(pattern[i]);
                x++;
            }
        }
        if (temp.Count > 0)
        {
            string[] newPattern = temp.ToArray();
            pattern = newPattern;
        }
        else
        {
            throw new EmtpyPatternFileException();
        }
    }

    public bool check(string inn, int method, int group, int codeLine)
    {
        Regex newReg = new Regex(inn);
        Match m = newReg.Match(assemblyMethods[method].groups[group].code[codeLine]);
        GroupCollection grp = m.Groups;
        if (newReg.IsMatch(assemblyMethods[method].groups[group].code[codeLine]))
        {
            for (int i = 1; i < grp.Count; i++)
            {
                capturedVariables[grp[i].Name] = grp[i].Value;
            }
            return true;
        }
        else
        {
            return false;
        }
    }
    //'check-next' function
    public bool checkNext(string inn, int method, int group, int codeLine)
    {
        Regex newReg = new Regex(inn);
        Match m = newReg.Match(assemblyMethods[method].groups[group].code[codeLine]);
        GroupCollection g = m.Groups;
        if (newReg.IsMatch(assemblyMethods[method].groups[group].code[codeLine]))
        {
            for (int i = 1; i < g.Count; i++)
            {
                capturedVariables[g[i].Name] = g[i].Value;
            }
            return true;
        }
        else
        {
            return false;
        }
    }
    //reads in assembly code and parses through it removing filler & comments
    public void removeComments(string[] inputCode)
    {
        bool assemblyFile = false;

        int lineCount = 0;
        int i = 0;
        int j = 0;
        int x = 0;
        if(inputCode.Length < 1)
        {
            throw new EmptyAssemblyFileException();
        }
        foreach (string line in inputCode)
        {
            lineCount++;
            Match m = Regex.Match(line, rex);
            if (line.StartsWith("; Assembly listing for method "))
            {
                assemblyFile = true;
                i = 0;
                //create new function object
                assemblyMethod am = new assemblyMethod();
                am.functionName = line;
                assemblyMethods.Add(am);
                continue;
            }
            // "=====..." denotes the end of a function
            if (line.StartsWith("; ============="))
            {
                x++;
            }
            //iterate through the subsequent lines to find the functions and the commands to be added to the 2d Array
            //use a boolean to identify when the end of the function is found to create a new object type
            else if (line.StartsWith(";") || line.StartsWith("//"))
            {
                continue;
            }
            else if (String.IsNullOrEmpty(line))
            {
                continue;
            }
            else if (line.Length <= 15)
            {
                if(assemblyFile==true)
                {
                    try
                    {
                        //make a new methods object 
                        assemblyMethods[x].addGroup();
                        assemblyMethods[x].groups[i].name = line;
                        j++;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Error in adding to 2D array", ex);
                    }
                }
                else
                {
                    assemblyMethod am = new assemblyMethod();
                    am.functionName = "N/A";
                    assemblyMethods.Add(am);
                    assemblyMethods[x].addGroup();
                    assemblyMethods[x].groups[i].name = "N/A";
                    string a = Regex.Replace(line, @"\s+\w+\s+", "");
                    a = Regex.Replace(a, "\\s+", " ");
                    assemblyMethods[x].groups[i].code.Add(a);
                    assemblyMethods[x].groups[i].lineNum.Add(lineCount);
                }
            }
            else if (line.Contains(";;"))
            {
                i++;
            }
            else if (m.Success)
            {
                string a = Regex.Replace(line, @"\s+\w+\s+", "");
                a = Regex.Replace(a, "\\s+", " ");
                assemblyMethods[x].groups[i].code.Add(a);
                assemblyMethods[x].groups[i].lineNum.Add(lineCount);
                j++;
            }

            else
            {
                continue;
            }
        }
    }    
}

class EmtpyPatternFileException : Exception
{
    public EmtpyPatternFileException()
        : base(String.Format("Invalid Pattern File\nPattern File is Empty")){}
}
class EmptyAssemblyFileException : Exception
{
    public EmptyAssemblyFileException()
        : base(String.Format("Invalid Assembly File\nAssembly File is Empty")) { }
}
class InvalidPatternException : Exception
{
    public InvalidPatternException()
        : base(String.Format("Invalid Pattern\nPattern File Contains Invalid Pattern")){}
}
class notEnoughArgumentsException : Exception
{
    public notEnoughArgumentsException()
    : base(String.Format("Incorrect number of Arguments entered must be 2 files \n1) A pattern file\n2) An Assembly file\n$Program.exe pattern.txt assemblyCode.txt")) { }
}
class fileDoesntExistException : Exception
{
    public fileDoesntExistException(string message)
    : base(message) { }
}

public class assemblyMethod
{
    public string functionName = "";
    public List<assemblyCode> groups = new List<assemblyCode>();

    public void addGroup()
    {
        assemblyCode c = new assemblyCode();
        groups.Add(c);
    }
}
public class assemblyCode
{
    public string name = "";
    public List<string> code = new List<string>();
    public List<int> lineNum = new List<int>();
}
public class foundCode
{
    public string method = "";
    public string groupName = "";
    public List<string> code = new List<string>();
    public List<int> lineNum = new List<int>();
    
}
public class patternReturnInfo
{
    public List<foundCode> code = new List<foundCode>();
    public List<string> otherInfo = new List<string>();
}