using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EdmGen2;
using System.Reflection;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using System.Data.Metadata.Edm;
using System.Data.Entity.Design;

namespace pgEdmGen
{
  class Program : EdmGen2.EdmGen2
  {
    

    protected struct OperationsMode
    {
      public string TitleText;
      public Mode EdmGenMode;
      public Func<string[],Dictionary<string,string>> Method;
    }


    #region Utility path options
    
    protected static Dictionary<string, OperationsMode> opModeDict = new Dictionary<string,OperationsMode>(){
      {"/fromedmx" , new OperationsMode(){ TitleText="FromEdmx", 
        Method= delegate(string[] args){
          EdmGen2.EdmGen2.FromEdmx(args);
          return new Dictionary<string, string>(){
            {"result.txt","executed EdmGen2.EdmGen2.FromEdmx"}
          };
        },
        EdmGenMode= Mode.FromEdmx
      }},
      {"/toedmx" , new OperationsMode(){ TitleText="ToEdmx", 
        Method= delegate(string[] args){
          EdmGen2.EdmGen2.ToEdmx(args);
          return new Dictionary<string, string>(){
            {"result.txt","executed EdmGen2.EdmGen2.ToEdmx"}
          };
        },
        EdmGenMode= Mode.ToEdmx
      }},
      {"/modelgen" , new OperationsMode(){ TitleText="ModelGen", 
        Method= delegate(string[] args){
          EdmGen2.EdmGen2.ModelGen(args);
          return new Dictionary<string, string>(){
            {"result.txt","executed EdmGen2.EdmGen2.ModelGen"}
          };
        },
        EdmGenMode= Mode.ModelGen
      }},
      {"/viewgen" , new OperationsMode(){ TitleText="ViewGen", 
        Method= delegate(string[] args){
          EdmGen2.EdmGen2.ViewGen(args);
          return new Dictionary<string, string>(){
            {"result.txt","executed EdmGen2.EdmGen2.ViewGen"}
          };
        },
        EdmGenMode= Mode.ViewGen
      }},
      {"/codegen", new OperationsMode(){ TitleText="CodeGen", 
        Method= delegate(string[] args){
          EdmGen2.EdmGen2.CodeGen(args);
          return new Dictionary<string, string>(){
            {"result.txt","executed EdmGen2.EdmGen2.CodeGen"}
          };
        },
        EdmGenMode= Mode.CodeGen
      }},
      {"/validate" , new OperationsMode(){ TitleText="Validate", 
        Method= delegate(string[] args){
          EdmGen2.EdmGen2.Validate(args);
          return new Dictionary<string, string>(){
            {"result.txt","executed EdmGen2.EdmGen2.Validate"}
          };
        },
        EdmGenMode= Mode.Validate
      }},
      {"/retrofitmodel" , new OperationsMode(){ TitleText="RetrofitModel", 
        Method= delegate(string[] args){
          EdmGen2.EdmGen2.RetrofitModel(args);
          return new Dictionary<string, string>(){
            {"result.txt","executed EdmGen2.EdmGen2.RetrofitModel"}
          };
        },
        EdmGenMode= Mode.RetrofitModel
      }},
      {"/help" , new OperationsMode(){ TitleText="Help", 
        Method= delegate(string[] args){
          return new Dictionary<string, string>(){
            {"result.txt","executed Help"}
          };
        },
        EdmGenMode= Mode.Help
      }},
      {"/fullbuild" , new OperationsMode(){ TitleText="FullBuild", 
        Method= pgEdmGen.Program.FullBuild,
        EdmGenMode= Mode.Help
      }}  
    };

    #endregion

    protected new static void Main(string[] args) 
    {
      try
      {
        Dictionary<string, string> resultFiles = null;
        if (opModeDict.ContainsKey(args[0].ToLower()))
        {
          OperationsMode currentMode = opModeDict[args[0].ToLower()];
          resultFiles = currentMode.Method.Invoke(args);
          WriteFiles(resultFiles);
        }
        else {
          Console.WriteLine("Could not find method");
        }
      }
      catch (Exception ex)
      {
        Console.WriteLine(ex.Message);
      }      
    }

    protected static Dictionary<string,string> FullBuild(string[] args)
    {
      string modelName = args[1];
      Dictionary<string, string> resultFiles = new Dictionary<string, string>();
      StringWriter statusWriter = new StringWriter();
      Action<string> informAction = delegate(string s)
      {
        Console.WriteLine(s);
        statusWriter.WriteLine(s);
      };

      informAction("beginning ModelGen for " + args[1]);
      pgEdmGen.Program.ModelGen(args);
      informAction("modelgen completed for " + args[1]);
      informAction("validating model " + args[2]+".edmx");
      EdmGen2.EdmGen2.Validate(new string[] { "", args[2] + ".edmx" });
      informAction("validation complete for model " + args[2] + ".edmx");


      resultFiles.Add("result.txt", statusWriter.ToString());
      return resultFiles;
    }

    protected static void WriteFiles(Dictionary<string,string> aFileCollection)
    {
      FileInfo activeFile;
      FileStream activeFileStream;
      StreamWriter writer;
      foreach (string item in aFileCollection.Keys)
      {
        activeFile = new FileInfo(item);
        activeFile.Delete();
        activeFile = new FileInfo(item);
        using(activeFileStream = activeFile.OpenWrite()){
          writer = new StreamWriter(activeFileStream);
          writer.Write(aFileCollection[item]);
          writer.Close();
        }
      }
    }


    // code by MS
    new protected static void FromEdmx(string[] args)
    {
      FileInfo edmxFile;
      if (ParseEdmxFileArguments(args[1], out edmxFile))
      {
        FromEdmx(edmxFile);
      }
    }

    new protected static Dictionary<string,string> FromEdmx(FileInfo edmxFile)
    {
      XDocument xdoc = XDocument.Load(edmxFile.FullName);
      Dictionary<string, string> resultFiles = new Dictionary<string,string>();

      // select the ssdl element and write it out
      XElement ssdl = GetSsdlFromEdmx(xdoc);
      resultFiles.Add(GetFileNameWithNewExtension(edmxFile, ".ssdl"), ssdl.ToString());
      
      // select the csdl element, and write it out
      XElement csdl = GetCsdlFromEdmx(xdoc);
      resultFiles.Add(GetFileNameWithNewExtension(edmxFile, ".csdl"), csdl.ToString());

      // select the msl element and write it out
      XElement msl = GetMslFromEdmx(xdoc);
      resultFiles.Add(GetFileNameWithNewExtension(edmxFile, ".msl"), msl.ToString());

      return resultFiles;
    }

    new protected static Dictionary<string,string> ModelGen(string[] args)
    {
      if (args.Length < 4 || args.Length > 5)
      {
        PrintModelGenUsage();
        return new Dictionary<string, string>() { { "result.txt", "printed modelgen usage" } };
      }
      string connectionString = args[1];
      string provider = args[2];
      string modelName = args[3];
      Version version = EntityFrameworkVersions.Version2;
      if (args.Length > 4)
      {
        if (args[4] == "1.0")
        {
          version = EntityFrameworkVersions.Version1;
        }
      }

      bool includeForeignKeys = version == EntityFrameworkVersions.Version2 ? true : false;
      if (args.Length > 5)
      {
        if (version == EntityFrameworkVersions.Version2 && args[5] != "includeFKs")
        {
          includeForeignKeys = true;
        }
        else
        {
          PrintModelGenUsage();
          return new Dictionary<string, string>() { { "result.txt", "printed modelgen usage" } };
        }
      }
      return ModelGen(connectionString, provider, modelName, version, includeForeignKeys);
    }

    new protected static Dictionary<string,string> ModelGen(
      string connectionString, string provider, string modelName, Version version, bool includeForeignKeys)
    {
      Dictionary<string, string> resultFiles = new Dictionary<string, string>() {{"result.txt",""}};
      
      Action<string> delInform = delegate(string s)
      {
        Console.WriteLine(s);
        resultFiles["result.txt"] = new StringBuilder(resultFiles["result.txt"]).AppendLine(s).ToString();
      };

      IList<EdmSchemaError> ssdlErrors = null;
      IList<EdmSchemaError> csdlAndMslErrors = null;

      // generate the SSDL
      string ssdlNamespace = modelName + "Model.Store";
      EntityStoreSchemaGenerator essg =
        new EntityStoreSchemaGenerator(
          provider, connectionString, ssdlNamespace);
      essg.GenerateForeignKeyProperties = includeForeignKeys;

      ssdlErrors = essg.GenerateStoreMetadata(
        new List<EntityStoreSchemaFilterEntry>() { 
          new  EntityStoreSchemaFilterEntry(null,null,null,EntityStoreSchemaFilterObjectTypes.Function,EntityStoreSchemaFilterEffect.Exclude) ,
          new EntityStoreSchemaFilterEntry(null,"CreationArea",null,EntityStoreSchemaFilterObjectTypes.All,EntityStoreSchemaFilterEffect.Allow) 
        }
        , version);

      // detect if there are errors or only warnings from ssdl generation
      bool hasSsdlErrors = false;
      bool hasSsdlWarnings = false;
      if (ssdlErrors != null)
      {
        foreach (EdmSchemaError e in ssdlErrors)
        {
          if (e.Severity == EdmSchemaErrorSeverity.Error)
          {
            hasSsdlErrors = true;
          }
          else if (e.Severity == EdmSchemaErrorSeverity.Warning)
          {
            hasSsdlWarnings = true;
          }
        }
      }

      // write out errors & warnings
      if (hasSsdlErrors && hasSsdlWarnings)
      {
        System.Console.WriteLine("Errors occurred during generation:");
        WriteErrors(ssdlErrors);
      }

      // if there were errors abort.  Continue if there were only warnings
      if (hasSsdlErrors)
      {
        delInform("aborted because of ssdl errors");
        return resultFiles;
      }

      // write the SSDL to a string
      StringWriter ssdl = new StringWriter();
      XmlWriter ssdlxw = XmlWriter.Create(ssdl);
      essg.WriteStoreSchema(ssdlxw);
      ssdlxw.Flush();

      // generate the CSDL
      string csdlNamespace = modelName + "Model";
      string csdlEntityContainerName = modelName + "Entities";
      EntityModelSchemaGenerator emsg =
        new EntityModelSchemaGenerator(
          essg.EntityContainer, csdlNamespace, csdlEntityContainerName);
      emsg.GenerateForeignKeyProperties = includeForeignKeys;
      csdlAndMslErrors = emsg.GenerateMetadata(version);


      // detect if there are errors or only warnings from csdl/msl generation
      bool hasCsdlErrors = false;
      bool hasCsdlWarnings = false;
      if (csdlAndMslErrors != null)
      {
        foreach (EdmSchemaError e in csdlAndMslErrors)
        {
          if (e.Severity == EdmSchemaErrorSeverity.Error)
          {
            hasCsdlErrors = true;
          }
          else if (e.Severity == EdmSchemaErrorSeverity.Warning)
          {
            hasCsdlWarnings = true;
          }
        }
      }

      // write out errors & warnings
      if (hasCsdlErrors || hasCsdlWarnings)
      {
        System.Console.WriteLine("Errors occurred during generation:");
        WriteErrors(csdlAndMslErrors);
      }

      // if there were errors, abort.  Don't abort if there were only warnigns.  
      if (hasCsdlErrors)
      {
        delInform("aborted because of csdl errors");
        return resultFiles;
      }

      // write CSDL to a string
      StringWriter csdl = new StringWriter();
      XmlWriter csdlxw = XmlWriter.Create(csdl);
      emsg.WriteModelSchema(csdlxw);
      csdlxw.Flush();

      // write MSL to a string
      StringWriter msl = new StringWriter();
      XmlWriter mslxw = XmlWriter.Create(msl);
      emsg.WriteStorageMapping(mslxw);
      mslxw.Flush();

      // write csdl, ssdl & msl to the EDMX file

      Dictionary<string,string> toEdmxResult = ToEdmx(
        csdl.ToString(), ssdl.ToString(), msl.ToString(), new FileInfo(
          modelName + ".edmx"));
      resultFiles.Add(toEdmxResult.Keys.First(), toEdmxResult[toEdmxResult.Keys.First()]);
      return resultFiles;
    }

    new protected static Dictionary<string,string> ToEdmx(String c, String s, String m, FileInfo edmxFile)
    {
      Dictionary<string, string> resultFiles = new Dictionary<string, string>();


      // This will strip out any of the xml header info from the xml strings passed in 
      XDocument cDoc = XDocument.Load(new StringReader(c));
      c = cDoc.Root.ToString();
      XDocument sDoc = XDocument.Load(new StringReader(s));
      s = sDoc.Root.ToString();
      XDocument mDoc = XDocument.Load(new StringReader(m));
      // re-write the MSL so it will load in the EDM designer
      FixUpMslForEDMDesigner(mDoc.Root);
      m = mDoc.Root.ToString();

      // get the version to use - we use the root CSDL as the version. 
      Version v = _namespaceManager.GetVersionFromCSDLDocument(cDoc);
      XNamespace edmxNamespace = _namespaceManager.GetEDMXNamespaceForVersion(v);

      // the "Version" attribute in the Edmx element
      string edmxVersion = v.Major + "." + v.MajorRevision;

      StringBuilder sb = new StringBuilder();
      sb.Append("<edmx:Edmx Version=\"" + edmxVersion + "\"");
      sb.Append(" xmlns:edmx=\"" + edmxNamespace.NamespaceName + "\">");
      sb.Append(Environment.NewLine);
      sb.Append("<edmx:Runtime>");
      sb.Append(Environment.NewLine);
      sb.Append("<edmx:StorageModels>");
      sb.Append(Environment.NewLine);
      sb.Append(s);
      sb.Append(Environment.NewLine);
      sb.Append("</edmx:StorageModels>");
      sb.Append(Environment.NewLine);
      sb.Append("<edmx:ConceptualModels>");
      sb.Append(Environment.NewLine);
      sb.Append(c);
      sb.Append(Environment.NewLine);
      sb.Append("</edmx:ConceptualModels>");
      sb.Append(Environment.NewLine);
      sb.Append("<edmx:Mappings>");
      sb.Append(Environment.NewLine);
      sb.Append(m);
      sb.Append(Environment.NewLine);
      sb.Append("</edmx:Mappings>");
      sb.Append(Environment.NewLine);
      sb.Append("</edmx:Runtime>");
      sb.Append(Environment.NewLine);
      sb.Append("<edmx:Designer xmlns=\"" + edmxNamespace.NamespaceName + "\">");
      sb.Append(Environment.NewLine);
      sb.Append("<Connection><DesignerInfoPropertySet><DesignerProperty Name=\"MetadataArtifactProcessing\" Value=\"EmbedInOutputAssembly\" /></DesignerInfoPropertySet></Connection>");
      sb.Append(Environment.NewLine);
      sb.Append("<edmx:Options />");
      sb.Append(Environment.NewLine);
      sb.Append("<edmx:Diagrams />");
      sb.Append(Environment.NewLine);
      sb.Append("</edmx:Designer>");
      sb.Append("</edmx:Edmx>");
      sb.Append(Environment.NewLine);

      resultFiles.Add(edmxFile.FullName, sb.ToString());
      return resultFiles;
      //File.WriteAllText(edmxFile.FullName, sb.ToString());
    }
  }
}
