using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;


public class pattern_finder
{
    //text file used to save output of runs
    public StreamWriter file = new StreamWriter("C:/Users/2cath/source/repos/jitutils/assembly_patterns/WriteLines2.txt");
    //dictionary uses the captured variable name in the first string
    //and assigns its content in the second string
    public Dictionary<string, string> capturedVariables = new Dictionary<string, string>();

    public List<string> found = new List<string>();
    public List<string> tempFoundCode = new List<string>();
    public List<foundCode> foundCodeSnippets = new List<foundCode>();

    public List<assemblyMethod> assemblyMethods = new List<assemblyMethod>();
    List<Regex> inPattern = new List<Regex>();
    string[] pattern;
    //Regex[] inPattern;
    public string rex = @"\s*([0-9A-F]{8})\s+(\w+)\s+(.*)";
    public int counter = 0;
    public int methodsFound = 0;
    public int groupsFound = 0;

    public static void Main(string[] args)
    {
        Stopwatch watch = new Stopwatch();
        watch.Start();

        pattern_finder p = new pattern_finder();

        p.pattern = System.IO.File.ReadAllLines(args[0]);
        string[] assembly = System.IO.File.ReadAllLines(args[1]);

        //Pattern file is empty error ####
        try
        {
            p.patternCleaner();//removes extra spaces
        }
        catch(EmtpyPatternFileException ex)
        {
            Console.WriteLine(ex.Message);
            Environment.Exit(1);
        }
        //error catch for empty Pattern file


        p.inputToRegex(p.pattern); // converts to regex (combine in above method in future)

        p.removeComments(assembly); //removes comments and assigns line number of code from original file
        p.patternChecker();//main function checking the pattern against each line of code in the assembly file

        watch.Stop();

        //if a match is found prints output,
        if (p.counter > 0)
        {
            p.printFoundPatterns();
            Console.WriteLine("Matches Found = " + p.counter);
            p.file.WriteLine("Matches Found = " + p.counter);
            Console.WriteLine("time taken = " + watch.Elapsed.TotalSeconds + " secs.");
            p.file.WriteLine("time taken = " + watch.Elapsed.TotalSeconds + " secs.");
            Console.WriteLine("Pattern was found in:");
            p.file.WriteLine("Pattern was found in:");
            Console.WriteLine("\t" + p.methodsFound + " Methods. \n\t" + p.groupsFound + " groups.");
            p.file.WriteLine("\t" + p.methodsFound + " Methods. \n\t" + p.groupsFound + " groups.");

            p.file.Close();
        }
        //if no matches found possible error
        else
        {
            #warning "0 Matches found"
            Console.WriteLine("\n\n"+p.counter+ " Matches Found \n\nCheck your Pattern file for correct input");
        }
    }
    public void patternChecker()
    {
        //take a snapshot
        bool methodAdded = false;
        bool groupChanged = false;
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
                    string currPat = inPattern[currPatternLine].ToString();
                    string comp = Regex.Replace(currPat, @"^[^\s]+\s*", "");
                    string curCode = assemblyMethods[method].groups[group].code[currLine];
                    nextLine = currLine + 1;

                    if (currPat.Contains("?<<"))
                    {
                        Regex extractorRegex = new Regex(@"\?<<(\w+)>>");
                        MatchCollection extractedMatch = extractorRegex.Matches(currPat);
                        foreach (Match match in extractedMatch)
                        {
                            currPat = currPat.Replace(match.Captures[0].Value, capturedVariables[match.Groups[1].Value]);
                        }
                        comp = Regex.Replace(currPat, @"^[^\s]+\s*", "");
                    }

                    if (currPat.StartsWith("check-next:") && checkNext(comp, method, group, currLine))
                    {
                        string toAdd = assemblyMethods[method].groups[group].lineNum[currLine]+":\t "+ curCode;
                        tempFoundCode.Add(toAdd);
                        currPatternLine++;
                    }

                    else if (currPat.StartsWith("check-not:"))
                    {

                        currPatternLine++;
                    }

                    else if (currPat.StartsWith("check:") && check(comp, method, group, currLine) && currPatternLine == 0)
                    {
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
                        if (check(comp, method, group, currLine))
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
                    else if (currPatternLine == 0)
                    {
                        currLine = nextLine;
                        continue;
                    }
                    else
                    {
                        tempFoundCode.Clear();
                        capturedVariables.Clear();
                        nextLine = tempLine;
                        group = tempGroup;
                        method = tempMethod;
                        testAc = tempAc;
                        testAm = tempAm;

                        currPatternLine = 0;
                    
                    }
                    if (currPatternLine >= inPattern.Count) //successful pattern found, clear temp objects
                    {
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
                            string groupToAdd = "\n\t" + assemblyMethods[method].groups[group].name;
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
    public void printFoundPatterns()
    {
        List<string> toTextFile = new List<string>();
        

        foreach (foundCode fc in foundCodeSnippets)
        {
            if(fc.method != null)
            {
                toTextFile.Add(fc.method);
                Console.WriteLine(fc.method);
                methodsFound++;
            }
            if(fc.groupName != null)
            {
                toTextFile.Add(fc.groupName);
                Console.WriteLine(fc.groupName);
                groupsFound++;
            }
            foreach(string line in fc.code)
            {
                toTextFile.Add(line);
                Console.WriteLine("\t\t"+line);
            }
        }
        foreach (string line in toTextFile)
        {
            file.WriteLine(line);
        }
        
    }
    public void inputToRegex(string[] input)
    {
        foreach (string inn in input)
        {
            //cus = CleanedUpString
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
    //'check-not' function
    public bool checkNot(string ptrn)
    {
        return false;
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
        int lineCount = 0;
        int i = 0;
        int j = 0;
        int x = 0;
        foreach (string line in inputCode)
        {
            lineCount++;
            Match m = Regex.Match(line, rex);
            if (line.StartsWith("; Assembly listing for method "))
            {
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
                try
                {
                    //make a new methods object 
                    assemblyMethods[x].addGroup();
                    assemblyMethods[x].groups[i].name = line;
                    j++;
                    //am.methods[i][j] = line;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error in adding to 2D array", ex);
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
    //public EmtpyPatternFileException() { }

    public EmtpyPatternFileException()
        : base(String.Format("Invalid Pattern File\nPattern File is Empty"))
    {

    }
}

public class assemblyMethod
{
    public string functionName;
    public List<assemblyCode> groups = new List<assemblyCode>();

    public void addGroup()
    {
        assemblyCode c = new assemblyCode();
        groups.Add(c);
    }
}
public class assemblyCode
{
    public string name;
    public List<string> code = new List<string>();
    public List<int> lineNum = new List<int>();
}
public class foundCode
{
    public string method = null;
    public string groupName = null;
    public List<string> code = new List<string>();
    public List<int> lineNum = new List<int>();
}