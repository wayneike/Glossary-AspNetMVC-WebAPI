using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Xml;
using GlossaryWebAPI.Models;
using System.Web;
using System.Text;
using System.IO;

namespace IBISGlossaryWebAPI.Controllers
{
    public class GlossaryAPIController : ApiController
    {
        private string dataXmlPath = GlossaryWebAPI.Properties.Settings.Default.DataXMLPath;
        private List<GlossaryModel> GlossaryList = new List<GlossaryModel>();

        /// <summary>
        /// Returns alphabetically sorted glossary
        /// </summary>
        /// <returns></returns>
        public IEnumerable<GlossaryModel> GetAllGlossaryItems()
        {
            try
            {
                LoadDataFromXML();
            }
            catch (Exception e)
            {
                throw new HttpResponseException(XmlErrorMessage(e));
            }

            return GlossaryList.OrderBy(x => x.Term); // Sort alphabetically by Term
        }

        /// <summary>
        /// Finds term/definition by ID
        /// </summary>
        /// <param name="param">data ID</param>
        /// <returns>GlossaryModel</returns>
        public GlossaryModel GetGlossaryDefinitionByID(string param)
        {
            GlossaryModel glossaryItem;
            try
            {
                LoadDataFromXML();
                glossaryItem = GlossaryList.Find(g => g.ID.Equals(param, StringComparison.OrdinalIgnoreCase));
            }
            catch (Exception e)
            {
                throw new HttpResponseException(XmlErrorMessage(e));
            }

            return glossaryItem;
        }
 
        /// <summary>
        /// adds term/definition with auto generated id
        /// modified term/definition if term already exists in the glossary
        /// </summary>
        /// <param name="term"></param>
        /// <param name="definition"></param>
        /// <returns></returns>
        public HttpResponseMessage PostGlossaryItem(string definition, string term)
        {
            if (string.IsNullOrWhiteSpace(term))
            {
                HttpResponseMessage msg = new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = new StringContent("Term cannot be blank"),
                    ReasonPhrase = "Term cannot be blank!"
                };

                return msg;
            }

            try
            {
                LoadDataFromXML();

                GlossaryModel gm = GlossaryList.Find(g => g.Term.Equals(term, StringComparison.OrdinalIgnoreCase));

                if (gm == null)
                { // Add
                    gm = new GlossaryModel();
                    gm.Term = term;
                    gm.ID = System.Guid.NewGuid().ToString(); // can be anything as long as it's unique
                }
                else
                { // Term exists.  Update its description.
                    GlossaryList.Remove(gm); // Do not allow duplicate terms              
                }

                gm.Definition = definition;

                GlossaryList.Add(gm);
                WriteDataToXML(GlossaryList); // Persist data
            }
            catch (Exception e)
            {
                HttpResponseMessage msg = XmlErrorMessage(e);

                return msg;
            }

            return new HttpResponseMessage(HttpStatusCode.OK);
        }

        /// <summary>
        /// Modifies term/definition with data ID.  If ID was not found on the list, adds term/definition.
        /// If the new term already exists as another entry in the glossary, then let the user figure out what to do.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="term"></param>
        /// <param name="definition"></param>
        /// <returns></returns>
        public HttpResponseMessage PostModGlossaryItem(string definition, string id, string term)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                HttpResponseMessage msg = new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = new StringContent("Term cannot be blank"),
                    ReasonPhrase = "Term cannot be blank!"
                };

                return msg;
            }

            try
            {
                LoadDataFromXML();

                GlossaryModel gm = GlossaryList.Find(g => (g.Term.Equals(term, StringComparison.OrdinalIgnoreCase) && !g.ID.Equals(id, StringComparison.OrdinalIgnoreCase)));

                if (gm != null) // The corrected term has already been entered into the glossary.  Send the user a message.
                {
                    HttpResponseMessage msg = new HttpResponseMessage(HttpStatusCode.Conflict)
                    {
                        Content = new StringContent("Another entry with Term: " + term + " already exists"),
                        ReasonPhrase = "Another entry with Term: " + term + "already exists!"
                    };

                    return msg;
                }

                gm = GlossaryList.Find(g => g.ID.Equals(id, StringComparison.OrdinalIgnoreCase));

                if (gm == null) // unable to find its ID.  Create a new one.
                {
                    gm = new GlossaryModel();
                    gm.ID = System.Guid.NewGuid().ToString(); // can be anything as long as it's unique
                }
                else  // ID exists.  Modify term and definition
                {
                    GlossaryList.Remove(gm); // Do not allow duplicate terms   
                }

                gm.Term = term;
                gm.Definition = definition;

                GlossaryList.Add(gm);
                WriteDataToXML(GlossaryList); // Persist data
            }
            catch (Exception e)
            {
                HttpResponseMessage msg = XmlErrorMessage(e);

                return msg;
            }

            return new HttpResponseMessage(HttpStatusCode.OK);
        }

        /// <summary>
        /// deletes Term and its definition from Glossary
        /// </summary>
        /// <param name="param">Term to be deleted</param>
        /// <returns></returns>
        public HttpResponseMessage DeleteGlossaryItem(string param)
        {
            if (string.IsNullOrWhiteSpace(param))
            {
                HttpResponseMessage msg = new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = new StringContent("Term cannot be blank"),
                    ReasonPhrase = "Term cannot be blank!"
                };

                return msg;
            }

            try
            {
                LoadDataFromXML();

                // Clean up all duplicate terms while at it.
                GlossaryList.RemoveAll(x => x.Term.Equals(param, StringComparison.OrdinalIgnoreCase));
                WriteDataToXML(GlossaryList); // Persist data
            }
            catch (Exception e)
            {
                HttpResponseMessage msg = XmlErrorMessage(e);

                return msg;
            }

            return new HttpResponseMessage(HttpStatusCode.OK);
        }

        private HttpResponseMessage XmlErrorMessage(Exception e)
        {
            HttpResponseMessage msg = new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("Error with glossary data XML.\r\n\r\n" + e.Message),
                ReasonPhrase = "Error with glossary data XML. " + e.Message
            };

            return msg;
        }

        private void LoadDataFromXML()
        {
            GlossaryList.Clear();
            //Load XML from the file into XmlDocument object
            var filePath = HttpContext.Current.Server.MapPath(dataXmlPath);

            // Return instead of throwing an exception if the file doesn't exist.
            // Let WriteDataToXML create one later.
            if (!File.Exists(filePath))
            {
                return;
            }

            try
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(filePath);

                XmlNode root = doc.DocumentElement;
                StringBuilder sb = new StringBuilder();
                XmlNodeList nodeList = root.SelectNodes("Definition");
                foreach (XmlNode node in nodeList)
                {
                    string Definition = node.InnerText;

                    // Term and ID are mandatory fields.
                    // It should throw an exception if either one of these attributes are missing.
                    string Term = node.Attributes["Term"].Value;
                    string ID = node.Attributes["ID"].Value;

                    GlossaryModel gm = new GlossaryModel();
                    gm.Term = Term;
                    gm.Definition = Definition;
                    gm.ID = ID;

                    GlossaryList.Add(gm);
                }
            }
            catch (Exception e)
            {
                throw new Exception("Error parsing glossary data XML: " + e.Message);
            }
        }

        private void WriteDataToXML(List<GlossaryModel> gmList)
        {
            try
            {
                XmlDocument doc = new XmlDocument();
                doc.LoadXml("<Glossary></Glossary>");
                doc.PrependChild(doc.CreateXmlDeclaration("1.0", "utf-8", ""));
                XmlNode root = doc.DocumentElement;

                foreach (GlossaryModel gm in gmList)
                {
                    XmlElement Definition = doc.CreateElement("Definition");
                    Definition.SetAttribute("Term", gm.Term);
                    Definition.SetAttribute("ID", gm.ID);
                    Definition.InnerText = gm.Definition;
                    root.AppendChild(Definition);
                }

                doc.Save(HttpContext.Current.Server.MapPath(dataXmlPath));
            }
            catch (Exception e)
            {
                throw new Exception("Error generating glossary data XML: " + e.Message);
            }
        }
    }
}
