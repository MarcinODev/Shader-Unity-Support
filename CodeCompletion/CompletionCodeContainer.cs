using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/// <summary>
/// Container for code completion phrases (loaded from predefined files and from opened in editor files)
/// </summary>
public class CompletionCodeContainer
{
    public const string lowerDisplayName = "lowerDisplay";
    public const string isLocalPropName = "isLocalField";
    private static CompletionCodeContainer instance;
    private Dictionary<string, CompletionCodeContainerData> globalData;
    private List<Completion> globalCompletionList;
    private Dictionary<string, FileCompletionData> fileData;
    private HashSet<string> openedFilesKeywords;

    private CompletionCodeContainer()
    {
        openedFilesKeywords = new HashSet<string>();
        fileData = new Dictionary<string,FileCompletionData>();
        globalData = new Dictionary<string, CompletionCodeContainerData>();
        FillDictWithDataFrom(globalData, Resource.AutoLight_cginc);
        FillDictWithDataFrom(globalData, Resource.TerrainEngine_cginc);
        FillDictWithDataFrom(globalData, Resource.Tessellation_cginc);
        FillDictWithDataFrom(globalData, Resource.Lighting_cginc);
        FillDictWithDataFrom(globalData, Resource.UnityCG_cginc);
        FillDictWithDataFrom(globalData, Resource.UnityCG_glslinc);
        FillDictWithDataFrom(globalData, Resource.UnityShaderVariables_cginc);
        FillDictWithDataFrom(globalData, Resource.LanguageData);

        globalCompletionList = GenerateCompletionListFromDict(globalData);
        globalCompletionList.Sort(CompletionSort);
    }

    private void FillDictWithDataFrom(Dictionary<string, CompletionCodeContainerData> dict, string str)
    {
        string[] lines = str.Split('\n');
        for(int i = 0; i < lines.Length; i++)
        {
            string[] split = lines[i].Split('@');
            if(split.Length < 3)
			{
				continue;
			}

			string display = split[0]; 
            string insert = split[1];
            if(split.Length == 3)
            {
                int commentIndex = split[2].IndexOf("//");
                int minusIndex = split[2].IndexOf("---");
                if( (minusIndex > 10 || minusIndex == -1) && (commentIndex > 10 || commentIndex == -1))
				{
					dict[display] = new CompletionCodeContainerData(display, insert, split[2]);
				}
			}
            else
            {
                StringBuilder sb = new StringBuilder(split[3]);
                i++;
                for(; i < lines.Length; i++)
                {
                    if(lines[i].Contains('@'))
                    {
                        sb.AppendLine(lines[i].Replace("@",""));
                        break;
                    }
                    else
					{
						sb.AppendLine(lines[i]);
					}
				}
                dict[display] = new CompletionCodeContainerData(display, insert, sb.ToString());
            }
        }
    }

    private List<Completion> GenerateCompletionListFromDict(Dictionary<string, CompletionCodeContainerData> data, bool isLocalFile = false)
    {
        List<Completion> list = new List<Completion>();
        foreach(var pair in data)
        {
            if(pair.Value.display.Split('(')[0].Length > 40)
			{
				continue;
			}

			var compl = new Completion(pair.Value.display, pair.Value.insert, pair.Value.description, null, null);
            compl.Properties[lowerDisplayName] = compl.DisplayText.ToLower();
            compl.Properties[isLocalPropName] = isLocalFile;
            list.Add(compl);
        }
        return list;
    }

    public List<Completion> GetCompletionListForFile(string file, ITextBuffer textBuffer)
    {
        FileCompletionData fileCompletionData = null;
        if(!fileData.TryGetValue(file, out fileCompletionData))
        {
            fileCompletionData = new FileCompletionData();
            fileCompletionData.lastUpdateTime = new TimeSpan(0,0,0,0,0);
            fileData[file] = fileCompletionData;
        }

        if( (DateTime.Now.TimeOfDay - fileCompletionData.lastUpdateTime).TotalSeconds > 60.0 )
        {
            fileCompletionData.lastUpdateTime = DateTime.Now.TimeOfDay;
            fileCompletionData.completionList = new List<Completion>(GlobalCompletionList);
            string text = textBuffer.CurrentSnapshot.GetText();
            var lines = text.Split('\n');
            var completionDict = new Dictionary<string,CompletionCodeContainerData>();
            var splitters = new char[]{' ', '.', '\t', '\\', '/', ':', ',', '*', '+', '-', '!', '(', ')', '{', '}', '%', '&', '|', '"', ';', '\r'};
            for(int i = 0; i < lines.Length; i++)
            {
                var split = lines[i].Split(splitters);
                foreach(var s in split)
                {
                    if(string.IsNullOrEmpty(s) || s.Length < 2)
					{
						continue;
					}

					completionDict[s] = new CompletionCodeContainerData(s, s, "Example: " + lines[i].Replace("\t", ""));
                    openedFilesKeywords.Add(s);
                }
            }

            fileCompletionData.completionList.AddRange(GenerateCompletionListFromDict(completionDict, true));
            ClearDuplicatesFromCompletionList(ref fileCompletionData.completionList);
            fileCompletionData.completionList.Sort(CompletionSort);
        }
        return fileCompletionData.completionList;
    }

    private void ClearDuplicatesFromCompletionList(ref List<Completion> list)
    {
        Dictionary<string, Completion> dict = new Dictionary<string,Completion>();
        for(int i = 0; i < list.Count; i++)
        {
            dict[list[i].DisplayText] = list[i];
        }
        list.Clear();
        foreach(var pair in dict)
		{
			list.Add(pair.Value);
		}
	}

    public void AddFileToGlobalCompletionList(string file, ITextBuffer textBuffer)
    {
        globalCompletionList = GetCompletionListForFile(file, textBuffer);
    }

    private int CompletionSort(Completion x, Completion y)
    {
        if(x != null && y != null && x.DisplayText != null && y.DisplayText != null)
		{
			bool isOpenedFileKeywordX = openedFilesKeywords.Contains(x.DisplayText);
			bool isOpenedFileKeywordY = openedFilesKeywords.Contains(y.DisplayText);

			if(isOpenedFileKeywordX != isOpenedFileKeywordY)
			{
				return isOpenedFileKeywordX ? -1 : 1;
			}

			bool isLocalX = (bool)x.Properties[isLocalPropName];
            bool isLocalY = (bool)y.Properties[isLocalPropName];

			if(isLocalX != isLocalY)
				return isLocalX ? -1 : 1;

			return x.DisplayText.CompareTo(y.DisplayText);
		}

		if(x == null || x.DisplayText == null)
		{
			return 1;
		}

		if(y == null || y.DisplayText == null)
		{
			return -1;
		}

		return 0;
    }

    public List<Completion> GlobalCompletionList
    {
        get { return globalCompletionList; }
    }

    public static CompletionCodeContainer Instance
    {
        get
        {
            if(instance == null)
            {
                instance = new CompletionCodeContainer();
            }
            return instance;
        }
    }
}

public class CompletionCodeContainerData
{
    public CompletionCodeContainerData(string disp, string ins, string desc)
    {
        display = disp;
        insert = ins;
        description = desc;
    }
    public string display;
    public string insert;
    public string description;
}

public class FileCompletionData
{
    public TimeSpan lastUpdateTime;
    public List<Completion> completionList;
}