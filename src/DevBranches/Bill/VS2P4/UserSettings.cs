using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml;
using System.Xml.Linq;

namespace BruSoft.VS2P4
{
    /// <summary>
    /// UserSettings provides default values for Perforce connection settings.  A set of UserSettings has an associated
    /// context, specified when the UserSettings is constructed.  The current UserSettings set for a given context and name is
    /// saved and can be retrieved at any time.
    /// 
    /// The settings values are read from an XML file stored in the user's local data folder, under a VS2P4 folder.
    /// </summary>
    public class UserSettings
    {
        /// <summary>
        /// Holds the current settings for a given context.  Whenever a UserSettings is constructed for a given
        /// context, that instance replaces any existing set for that context.
        /// </summary>
        private static Dictionary<String, UserSettings> currentContextSettings = new Dictionary<string,UserSettings>();
        private const string APP_DATA_PATH = @"VS2P4\user_settings.xml";    // Relative to %APPDATA%
        private static XmlReader settingsXml;

        /// <summary>
        /// The UserSettingsXML static property is primarily used for unit testing this class.  If not
        /// set, it retrieves an XmlReader for the user_settings.xml file in the %APP_DATA%\VS2P4 folder,
        /// if it exists.  If that file does not exist, it returns null.
        /// 
        /// If this property is set, it should be set to an XmlReader for XML that conforms to the
        /// DefaultUserSettings.xsd schema.
        /// </summary>
        public static XmlReader UserSettingsXML
        {
            get
            {
                if (settingsXml == null)
                {
                    // Get the default file, if it exists, and open a reader for it.  If that
                    // file does not exist, then we'll just return null.
                    string      settingsFilePath = GetDefaultFilePath();

                    if (File.Exists(settingsFilePath))
                    {
                        using (StreamReader reader = new StreamReader(settingsFilePath))
                        {
                            XmlReader xmlReader = XmlReader.Create(reader);

                            settingsXml = xmlReader;
                            try
                            {
                                ReadSettingsXml();

                            }
                            catch (Exception ex)
                            {
                                throw new UserSettingsException("Invalid user settings XML", ex);
                            }
                        }
                    }
                }
                return settingsXml;
            }

            set
            {
                settingsXml = value;
                ReadSettingsXml();
            }
        }

        public enum SettingsContext
        {
            UNIT_TEST,
            VISUAL_STUDIO
        };

        private SettingsContext context;
        private string groupName;
        private Nullable<bool> useP4Config;
        private string perforceServer;
        private string perforceUser;
        private string perforceWorkspace;
        private string workspacePath;

        public class UserSettingsException : Exception
        {
            public UserSettingsException(string message)
                : base(message)
            {
            }

            public UserSettingsException(string message, Exception baseException)
                : base(message, baseException)
            {
            }
        }

        /// <summary>
        /// GetSettings returns a UserSettings object from the UserSettingsXML static property
        /// that matches the specified group name.  If no such group exists, returns null.
        /// 
        /// If there is an error parsing XML, a UserSettingsException could be thrown,
        /// though this would only occur if the UserSettingsXML property has never been set and
        /// there is either an error in the default XML or no default file exists.
        /// </summary>
        /// <param name="groupName"></param>
        /// <returns></returns>
        public static UserSettings GetSettings(string groupName)
        {
            // If no settings XML has been defined, get the defaults.
            if (settingsXml == null)
            {
                XmlReader dummy = UserSettingsXML;
            }
            if (currentContextSettings.ContainsKey(groupName))
            {
                return currentContextSettings[groupName];
            }
            return null;
        }

        /// <summary>
        /// Returns the context specified for this settings group.
        /// </summary>
        public SettingsContext Context
        {
            get
            {
                return this.context;
            }
        }

        /// <summary>
        /// Return this settings group's name.
        /// </summary>
        public string Name
        {
            get
            {
                return groupName;
            }
        }

        /// <summary>
        /// Return true if this settings group specifies using the P4CONFIG environment variable, and false
        /// if it explicitly specifies not using it.  Return null if the settings group does not specify a
        /// setting.
        /// </summary>
        public Nullable<bool> UseP4Config
        {
            get
            {
                return useP4Config;
            }
        }

        /// <summary>
        /// Return the Perforce server and port to be used for connections to Perforce.
        /// </summary>
        public string PerforceServer
        {
            get
            {
                return perforceServer;
            }
        }

        /// <summary>
        /// Return the username for connections to Perforce.
        /// </summary>
        public string PerforceUser
        {
            get
            {
                return perforceUser;
            }
        }

        /// <summary>
        /// Return the name of the workspace to use in Perforce connections.
        /// </summary>
        public string PerforceWorkspace
        {
            get
            {
                return perforceWorkspace;
            }
        }

        /// <summary>
        /// Return the path to the workspace specified in the Perforce connection.  Primarily this
        /// only needs to be specified if you specify using P4CONFIG, and then only if somehow the
        /// the solution path does not match the Perforce client root path.
        /// 
        /// This attribute may also be used in unit tests of VS2P4 itself.
        /// </summary>
        public string WorkspacePath
        {
            get
            {
                return workspacePath;
            }
        }

        private XAttribute GetOptionalElementAttribute(XElement parent, string nodeName, string attrName)
        {
            string fullNodeName = "{http://tempuri.org/DefaultUserSettings.xsd}" + nodeName;
            var node = parent.Element(fullNodeName);
            if (node != null)
            {
                var attr = node.Attribute(attrName);
                if (attr != null)
                {
                    return attr;
                }
            }
            return null;
        }

        private UserSettings(SettingsContext context, string groupName, XElement groupElement)
        {
            this.context = context;
            this.groupName = groupName;
            // Default UseP4Config to null, in case it was never specified.
            this.useP4Config = null;

            // Now get the Perforce properties from the node.  Not all of them need exist.
            var attr = GetOptionalElementAttribute(groupElement, "UseP4Config", "value");

            if (attr != null)
            {
                this.useP4Config = attr.Value == "true" ? true : false;
            }
            attr = GetOptionalElementAttribute(groupElement, "PerforceServer", "value");
            if (attr != null)
            {
                this.perforceServer = attr.Value;
            }
            attr = GetOptionalElementAttribute(groupElement, "PerforceWorkspace", "value");
            if (attr != null)
            {
                this.perforceWorkspace = attr.Value;
            }
            attr = GetOptionalElementAttribute(groupElement, "PerforceUser", "value");
            if (attr != null)
            {
                this.perforceUser = attr.Value;
            }
            attr = GetOptionalElementAttribute(groupElement, "WorkspacePath", "value");
            if (attr != null)
            {
                this.workspacePath = attr.Value;
            }
        }

        private static string GetDefaultFilePath()
        {
            string appData = Environment.GetEnvironmentVariable("APPDATA");

            if (appData == null)
            {
                throw new UserSettingsException("APPDATA environment variable undefined.");
            }

            return Path.Combine(appData, APP_DATA_PATH);
        }

        private static string ContextToString(SettingsContext context)
        {
            if (context == SettingsContext.UNIT_TEST)
            {
                return "UnitTest";
            }
            else
            {
                return "VisualStudio";
            }
        }

        private static SettingsContext StringToContext(string context)
        {
            if (context == "UnitTest")
            {
                return SettingsContext.UNIT_TEST;
            }
            if (context == "VisualStudio")
            {
                return SettingsContext.VISUAL_STUDIO;
            }
            throw new ArgumentException(String.Format("Invalid string for context: {0}", context));
        }

        /// <summary>
        /// This method reads the XML from settingsXml and stores all the groups found there in
        /// the currentContextSettings collection.
        /// </summary>
        private static void ReadSettingsXml()
        {
            if (settingsXml != null)
            {
                XDocument xdoc = XDocument.Load(settingsXml);
                var getRoot = from contextNode in xdoc.Element("{http://tempuri.org/DefaultUserSettings.xsd}DefaultUserSettings")
                                                      .Elements("{http://tempuri.org/DefaultUserSettings.xsd}Group")
                              select contextNode;
                foreach (XElement groupElement in getRoot)
                {
                    string groupName = groupElement.Attribute("name").Value;
                    SettingsContext groupContext = StringToContext(groupElement.Attribute("context").Value);

                    currentContextSettings[groupName] = new UserSettings(groupContext, groupName, groupElement);
                }
            }
        }
    }
}
