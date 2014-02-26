using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace sema2012m
{
    public static class ONames
    {
        public static XName GetXName(string uristring)
        {
            if (uristring.Contains("://")) // Нормальный URI 
            {
                int pos = uristring.LastIndexOfAny(new char[] { '/', '#' });
                if (pos == -1) { throw new Exception("Error: strange construction of type string: " + uristring); }
                XName xn = XName.Get(uristring.Substring(pos + 1), uristring.Substring(0, pos + 1));
                return xn;
            }
            else if (uristring.Contains(':')) // Префикс и локальное имя
            {
                return XName.Get(uristring.Replace(':', '_').Replace('$', '_'));
            }
            else // Без пространства имен
            {
                return XName.Get(uristring);
            }
        }
        //public static XName GetXName(string prop)
        //{
        //    int pos = prop.LastIndexOf('#'); if (pos == -1) pos = prop.LastIndexOf('/');
        //    XName xprop = XName.Get(prop.Substring(pos + 1), prop.Substring(0, pos + 1));
        //    return xprop;
        //}
        public static string rdfnsstring = "http://www.w3.org/1999/02/22-rdf-syntax-ns#";
        public static XNamespace rdfns = "http://www.w3.org/1999/02/22-rdf-syntax-ns#";
        public static XName xmllang = "{http://www.w3.org/XML/1998/namespace}lang";
        public static XName rdfabout = "{http://www.w3.org/1999/02/22-rdf-syntax-ns#}about";
        public static XName rdfresource = "{http://www.w3.org/1999/02/22-rdf-syntax-ns#}resource";
        public static XName rdfRDF = XName.Get("RDF", rdfnsstring);
        public static XName rdfdescription = "{http://www.w3.org/1999/02/22-rdf-syntax-ns#}Description";
        public static XName AttRdf = XName.Get("rdf", XNamespace.Xmlns.NamespaceName);
        public static string rdftypestring = "http://www.w3.org/1999/02/22-rdf-syntax-ns#type";
        public static XName xNameDatatypeProperty = XName.Get("name", sema2012m.ONames.FOG);


        public static XName TagDelete = XName.Get("delete", FOG);
        public static XName TagSubstitute = XName.Get("substitute", FOG);
        public static XName AttItem_id = "id";
        public static XName AttOld_id = "old-id";
        public static XName AttNew_id = "new-id";
        public static XName AttModificationTime = "mT";
        //public static XName AttUsersWorkRDFDocumentId = "UD";
        private static string FOGI = "{http://fogid.net/o/}";
        public static XName tag_name = FOGI + "name";
        public static XName tag_filestore = "{http://fogid.net/o/}FileStore";
        public static XName tag_fordocument = FOGI + "forDocument";
        public static XName tag_uri = FOGI + "uri";
        public static XName tag_owner = FOGI + "owner";
        public static XName tag_contenttype = FOGI + "contentType";
        public static XName tag_docmetainfo = FOGI + "docmetainfo";

        public const string FOG = "http://fogid.net/o/";
        public const string FOGEntity = "http://fogid.net/e/";

        public const string p_father = FOG + "father";
        public const string p_mother = FOG + "mother";
        public const string p_husband = FOG + "husband";
        public const string p_wife = FOG + "wife";
        public const string p_incollection = FOG + "in-collection";
        public const string p_collectionitem = FOG + "collection-item";
        public const string p_indoc = FOG + "in-doc";
        public const string p_reflected = FOG + "reflected";
        public const string p_inorg = FOG + "in-org";
        public const string p_roleclassification = FOG + "role-classification";
        public const string p_participant = FOG + "participant";
        public const string p_orgclassification = FOG + "org-classification";
        public const string p_name = FOG + "name";
        public const string p_fromdate = FOG + "from-date";
        public const string p_todate = FOG + "to-date";
        public const string p_alias = FOG + "alias";
        public const string p_referredsys = FOG + "referred-sys";
        public const string p_description = FOG + "description";
        public const string p_ground = FOG + "ground";
        public const string p_fordocument = FOG + "forDocument";
        public const string p_role = FOG + "role";
        public const string p_hastitle = FOG + "has-title";
        public const string p_degree = FOG + "degree";
        public const string p_orgparent = FOG + "org-parent";
        public const string p_orgchild = FOG + "org-child";
        public const string p_author = FOG + "author";
        public const string p_adoc = FOG + "adoc";
        public const string p_archiveitem = FOG + "archive-item";
        public const string p_inarchive = FOG + "in-archive";
        public const string p_infosource = FOG + "info-source";
        public const string p_referred = FOG + "referred";
        public const string p_somedate = FOG + "some-date";
        public const string p_date = FOG + "date";
        public const string p_ = FOG + "";

        public const string t_person = FOG + "person";
        public const string t_document = FOG + "document";
        public const string t_photodoc = FOG + "photo-doc";
        public const string t_videodoc = FOG + "videodoc";
        public const string t_audiodoc = FOG + "audiodoc";
        public const string t_speech = FOG + "speech";
        public const string t_ = FOG + "";
    }
}
