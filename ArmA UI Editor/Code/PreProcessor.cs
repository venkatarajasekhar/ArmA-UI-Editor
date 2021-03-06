﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using NLog;

namespace ArmA_UI_Editor.Code
{
    public class PreProcessFile
    {
        public class PPDefine
        {
            private string _name;
            private string[] _arguments;
            private string _value;
            public string Name { get { return this._name; } }
            public string[] Arguments { get { return this._arguments; } }
            public string Value { get { return this._value; } }

            public PPDefine(string s)
            {
                int spaceIndex = s.IndexOf(' ');
                if (spaceIndex == -1)
                {//No Space found thus we just have a define here without anything else
                    _name = s;
                    _arguments = new string[0];
                    _value = "";
                    return;
                }
                int bracketsIndex = s.IndexOf('(');
                if (spaceIndex < bracketsIndex || bracketsIndex == -1)
                {//first bracket was found after first space OR is not existing thus we have a simple define with a replace value here
                    _name = s.Remove(spaceIndex);
                    _arguments = new string[0];
                    _value = s.Remove(0, spaceIndex + 1);
                    return;
                }
                //we got a define with arguments here
                string argumentsString = s.Remove(0, bracketsIndex + 1);
                argumentsString = argumentsString.Remove(argumentsString.IndexOf(')'));
                _arguments = argumentsString.Split(',');
                for (int i = 0; i < _arguments.Length; i++)
                {
                    _arguments[i] = _arguments[i].Trim();
                }
                _name = s.Remove(bracketsIndex);
                bracketsIndex = s.IndexOf(") ");
                if (bracketsIndex == -1)
                    throw new Exception("Missing character to close argument list ')' or no value for argument define");
                _value = s.Remove(0, bracketsIndex + 2);
            }
            public PPDefine(string name, string argumentString, string value)
            {
                _name = name;
                _arguments = argumentString.Trim(new char[] { '(', ')' }).Split(',');
                _value = value;
                for (int i = 0; i < _arguments.Length; i++)
                {
                    _arguments[i] = _arguments[i].Trim();
                }
            }
            private string defineContentReplace(string argText, string value, string input)
            {
                string output = "";
                string word = "";
                for (int i = 0; i < input.Length; i++)
                {
                    char c = input[i];
                    if (!char.IsLetterOrDigit(c) && c != '_')
                    {
                        //reset current word as we did not had the current define here and encountered a word terminator
                        output += word + c;
                        word = "";
                        continue;
                    }
                    word += c;

                    if (word.Equals(argText, StringComparison.Ordinal))
                    {
                        if (i + 1 < input.Length)
                        {
                            char cLH = input[i + 1];
                            if (cLH == '\t' || cLH == ' ' || cLH == '\r' || cLH == '\n')
                            {

                            }
                            else if (cLH == '#')
                            {
                                if (i + 2 < input.Length)
                                {
                                    if (input[i + 2] != '#')
                                        continue;
                                    else
                                        i += 2;
                                }
                                else
                                {
                                    continue;
                                }
                            }
                            else if (!char.IsLetterOrDigit(c))
                            {
                                continue;
                            }
                        }
                        output += value;
                        word = "";
                    }
                }
                return output + word;
            }

            public string replace(string input)
            {
                string output = "";
                string word = "";
                //Itterate through EVERY character (as we cant simply use the normal string.replace function we have to do it by ourself ...)
                for (int i = 0; i < input.Length; i++)
                {
                    char c = input[i];
                    //check if our current character is a letter or number or _
                    if (!char.IsLetterOrDigit(c) && c != '_')
                    {
                        //reset current word as we did not had the current define here and encountered a word terminator
                        output += word + c;
                        word = "";
                        continue;
                    }
                    //add current character to current word
                    word += c;
                    //check if current word matches our define
                    if (word.Equals(_name, StringComparison.Ordinal))
                    {
                        //it matched our define
                        //check if our current define has arguments and directly replace the word with current content if it has no
                        if (_arguments.Length == 0)
                        {
                            word = _value;
                            output += word;
                            word = "";
                            continue;
                        }
                        //we do have arguments so lets continue

                        i++;
                        //make sure our word has arguments attached and throw an exception if not
                        if (input[i] != '(')
                            throw new Exception("encountered unexpected character while preprocessing, expected '(' but got '" + c + "'");
                        //some variables to set before the for loop
                        string curValue = _value;
                        int curArg = 0;
                        word = "";
                        int counter = 1;
                        char ignoreStringChar = '\0';
                        bool ignoreString = false;
                        int backSlashCounter = 0;

                        //Lets go and parse everything we can find that could be a part of our define
                        for (i++; i < input.Length; i++)
                        {
                            c = input[i];
                            //check if we have an argument seperator here
                            if (c == ',' && counter == 1 && !ignoreString)
                            {
                                //throw an expection if we have too many arguments for this define
                                if (curArg >= _arguments.Length)
                                    throw new Exception("encountered unexpected extra argument in define while preprocessing, allowed count is " + _arguments.Length);
                                curValue = this.defineContentReplace(_arguments[curArg], word, curValue);//curValue.Replace(_arguments[curArg], word);
                                word = "";
                                curArg++;
                                continue;
                            }
                            //check for the end of arguments here
                            if (c == ')' && counter == 1 && !ignoreString)
                            {
                                //throw an expection if we have too many arguments for this define
                                if (curArg >= _arguments.Length)
                                    throw new Exception("encountered unexpected extra argument in define while preprocessing, allowed count is " + _arguments.Length);
                                curValue = this.defineContentReplace(_arguments[curArg], word, curValue);//curValue.Replace(_arguments[curArg], word);
                                word = "";
                                counter--;
                                break;
                            }
                            //add current character to current argument word
                            word += c;
                            if (ignoreString)
                            {
                                //We are currently inside a string so lets ignore every possible character that could annoy us
                                if (c == ignoreStringChar && backSlashCounter % 2 == 0)
                                {
                                    ignoreString = false;
                                    ignoreStringChar = '\0';
                                    continue;
                                }
                                //Add +1 to the backSlashCounter so we can make sure that we wont accidently exit the string too early and reset it if we dont have a backslash here
                                if (c == '\\')
                                    backSlashCounter++;
                                else
                                    backSlashCounter = 0;
                            }
                            else
                            {
                                //Check for string mode
                                if (c == '\'' || c == '"')
                                {
                                    ignoreString = true;
                                    ignoreStringChar = c;
                                    continue;
                                }
                                //Check if we have another capsulation here, if we do add +1 to the counter
                                if (c == '(' || c == '{')
                                {
                                    counter++;
                                    continue;
                                }
                                //End of an capsulation, if it is remove -1 from the counter
                                if (c == ')' || c == '}')
                                {
                                    counter--;
                                    continue;
                                }
                            }
                        }
                        //we exited the define with an invalid number of capsulations ... seems like something moved wrong here!
                        if (counter != 0)
                            throw new Exception("Missing macro arguments end character in current line");
                        output += curValue;
                    }
                }
                output += word;
                return output;
            }
        }
        private static Logger Logger = LogManager.GetCurrentClassLogger();
        public enum preprocessFile_IfDefModes
        {
            TRUE = 0,
            FALSE,
            IGNORE
        }
        private MemoryStream fileStream;
        public MemoryStream FileStream { get { return this.fileStream; } }
        private string filePath;
        public string FilePath { get { return filePath; } }
        private string name;
        public string Name { get { return name; } }

        public PreProcessFile(string path, string name)
        {
            this.fileStream = new MemoryStream();
            this.filePath = path;
            this.name = name;
        }
        public void resetPosition()
        {
            this.fileStream.Seek(0, SeekOrigin.Begin);
        }
        public override string ToString()
        {
            return filePath;
        }


        public static bool PerformPreProcessFile(string filePath, List<PreProcessFile> ppFiles, List<preprocessFile_IfDefModes> ifdefs = null, string name = "", Dictionary<string, PPDefine> defines = null)
        {
            if (name == "")
                name = filePath.Substring(filePath.LastIndexOf('\\') + 1);
            if (ifdefs == null)
                ifdefs = new List<preprocessFile_IfDefModes>();
            if (defines == null)
                defines = new Dictionary<string, PPDefine>();
            //Open given file
            StreamReader reader = new StreamReader(filePath);
            PreProcessFile ppFile = new PreProcessFile(filePath, name);
            if (ppFiles != null)
                ppFiles.Add(ppFile);
            StreamWriter writer = new StreamWriter(ppFile.FileStream);

            //Prepare some variables needed for the entire processing periode in this function
            string s;
            uint filelinenumber = 0;
            while ((s = reader.ReadLine()) != null)
            {
                filelinenumber++;
                //skip empty lines
                if (string.IsNullOrWhiteSpace(s))
                {
                    writer.WriteLine();
                    continue;
                }
                //Remove left & right whitespaces and tabs from current string
                string sTrimmed = s.TrimStart();
                string leading = s.Substring(0, s.Length - sTrimmed.Length);
                s = sTrimmed;
                if (s[0] != '#')
                {//Current line is no define, thus handle it normally (find & replace)
                 //Make sure we are not inside of an ifdef/ifndef that disallows further processing of following lines
                    int i = ifdefs.Count - 1;
                    if (i >= 0 && ifdefs[i] != preprocessFile_IfDefModes.TRUE)
                        continue;
                    try
                    {
                        //Let every define check if it is inside of current line
                        foreach (PPDefine def in defines.Values)
                            s = def.replace(s);
                    }
                    catch (Exception ex)
                    {
                        //Catch possible exceptions from define parsing
                        Logger.Error(string.Concat("Experienced some error while parsing existing defines. ", ex.Message, ". file: ", filePath, ". linenumber: ", filelinenumber));
                        reader.Close();
                        return false;
                    }
                    writer.WriteLine(leading + s);
                    continue;
                }
                //We DO have a define here
                //get end of the define name
                int spaceIndex = s.IndexOf(' ');
                if (spaceIndex < 0)
                    spaceIndex = s.Length;
                //set some required variables for the switch
                int index = -1;
                int index2 = -1;
                //get text AFTER the define
                string afterDefine = s.Substring(spaceIndex).TrimStart();

                writer.WriteLine();

                //Check which define was used
                switch (s.Substring(0, spaceIndex))
                {
                    default:
                        throw new Exception("Encountered unknown define '" + s.Substring(0, spaceIndex) + "'");
                    case "#include":
                        //We are supposed to include a new file at this spot so lets do it
                        //Beautify the filepath so we can work with it
                        afterDefine.Trim();
                        string newFile;
                        newFile = afterDefine.Trim(new char[] { '"', '\'', ' ' });
                        //make sure we have no self reference here
                        if (newFile.Equals(filePath, StringComparison.OrdinalIgnoreCase))
                        {
                            //Ohhh no ... some problem in OSI layer 8
                            reader.Close();
                            writer.Close();
                            throw new Exception("Include contains self reference. file: " + filePath + ". linenumber: " + filelinenumber);
                        }
                        //process the file before continuing with this
                        try
                        {
                            if (!PerformPreProcessFile(newFile, ppFiles, ifdefs, afterDefine.Trim(new char[] { '<', '>', '"', '\'', ' ' }), defines))
                            {
                                //A sub file encountered an error, so stop here to prevent useles waste of ressources
                                reader.Close();
                                writer.Close();
                                return false;
                            }
                        }
                        catch (Exception e)
                        {
                            throw new Exception(e.Message + ", from " + filePath);
                        }
                        break;
                    case "#define":
                        //The user wants to define something here
                        while (s.EndsWith("\\"))
                        {
                            writer.WriteLine();
                            afterDefine += reader.ReadLine();
                            filelinenumber++;
                        }
                        //Get the two possible characters index that can be encountered after a define
                        index = afterDefine.IndexOf(' ');
                        index2 = afterDefine.IndexOf('(');
                        //check which one is found first
                        if (index < 0 || (index2 < index && index2 >= 0))
                            index = afterDefine.IndexOf('(');
                        //check that we really got a define with a value here, if not just take the entire length as no value is needed and only value provided
                        if (index < 0)
                            index = afterDefine.Length;
                        if (defines.ContainsKey(afterDefine.Substring(0, index)))
                        {
                            //Redefining something is not allowed, so throw an error here
                            reader.Close();
                            writer.Close();
                            throw new Exception("Redefining a define is not allowed! Use #undefine to undef something. file: " + filePath + ". linenumber: " + filelinenumber);
                        }
                        //FINALLY add the define
                        defines.Add(afterDefine.Substring(0, index), new PPDefine(afterDefine));
                        break;
                    case "#undefine":
                        //just remove straigth
                        defines.Remove(s.Substring(spaceIndex).Trim());
                        break;
                    case "#ifdef":
                        //do required stuff for define ifs
                        if (defines.ContainsKey(afterDefine))
                            ifdefs.Add(ifdefs.Count == 0 || ifdefs[ifdefs.Count - 1] == preprocessFile_IfDefModes.TRUE ? preprocessFile_IfDefModes.TRUE : preprocessFile_IfDefModes.IGNORE);
                        else
                            ifdefs.Add(ifdefs.Count == 0 || ifdefs[ifdefs.Count - 1] == preprocessFile_IfDefModes.TRUE ? preprocessFile_IfDefModes.FALSE : preprocessFile_IfDefModes.IGNORE);
                        break;
                    case "#ifndef":
                        //do required stuff for define ifs
                        if (defines.ContainsKey(afterDefine))
                            ifdefs.Add(ifdefs.Count == 0 || ifdefs[ifdefs.Count - 1] == preprocessFile_IfDefModes.TRUE ? preprocessFile_IfDefModes.FALSE : preprocessFile_IfDefModes.IGNORE);
                        else
                            ifdefs.Add(ifdefs.Count == 0 || ifdefs[ifdefs.Count - 1] == preprocessFile_IfDefModes.TRUE ? preprocessFile_IfDefModes.TRUE : preprocessFile_IfDefModes.IGNORE);
                        break;
                    case "#else":
                        //do required stuff for define ifs
                        index = ifdefs.Count - 1;
                        if (index < 0)
                        {
                            reader.Close();
                            writer.Close();
                            throw new Exception("unexpected #else. file: " + filePath + ". linenumber: " + filelinenumber);
                        }
                        //swap the value of currents if scope to the correct value
                        ifdefs[index] = (ifdefs[index] == preprocessFile_IfDefModes.TRUE ? preprocessFile_IfDefModes.FALSE : (ifdefs[index] == preprocessFile_IfDefModes.FALSE ? preprocessFile_IfDefModes.TRUE : preprocessFile_IfDefModes.IGNORE));
                        break;
                    case "#endif":
                        //do required stuff for define ifs
                        index = ifdefs.Count - 1;
                        if (index < 0)
                        {
                            reader.Close();
                            writer.Close();
                            throw new Exception("unexpected #endif. file: " + filePath + ". linenumber: " + filelinenumber);
                        }
                        //remove current if scope
                        ifdefs.RemoveAt(index);
                        break;
                }
            }
            reader.Close();
            writer.Flush();
            ppFile.resetPosition();
            return true;
        }
    }

}