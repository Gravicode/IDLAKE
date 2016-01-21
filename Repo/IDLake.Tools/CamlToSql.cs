//using Microsoft.SharePoint;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace IDLake.Tools
{
    public class CamlToSql
    {
        public static string CamlToSqlWhere(string CAML)
        {
            
            string sqlWhere = string.Empty;
            XmlDocument xmlDoc = new XmlDocument();
            XmlNodeList nodeList;

            //Add <Query> around the SPView.Query since a valid XML document requires a single root element.
            //and SPView.Query doesn't.
            xmlDoc.LoadXml("<Query>" + CAML + "</Query>");

            nodeList = xmlDoc.GetElementsByTagName("Where");

            if (nodeList.Count == 1)
            {
                XmlNode nodeWhere = nodeList[0];

                if (nodeWhere.HasChildNodes) //Should Always be the case
                {
                    StringBuilder sb = new StringBuilder();
                    bool isSuccess = ProcessWhereNode(nodeWhere, ref sb);
                    sqlWhere = sb.ToString();
                }
            }

            return sqlWhere;
        }
        private static bool ProcessWhereNode(XmlNode xmlNode, ref StringBuilder sb)
        {
            bool isSuccess = false;
            Stack<string> operatorStack = new Stack<string>();
            Queue<string> valueQueue = new Queue<string>();
            string previousOp = string.Empty;
            string strOperator = string.Empty;

            try
            {
                //Call a method to iterate "recursively" throught the nodes to get the values and operators.
                if (ProcessRecursiveWhereNode(xmlNode, "", "", ref operatorStack, ref valueQueue))
                {

                    // For each operator adding parenthesis before starting 
                    StringBuilder sbTmp = new StringBuilder();
                    operatorStack.ToList().ForEach(x => sbTmp.Append("("));
                    sb.Append(sbTmp.ToString());

                    while (valueQueue.Count > 0)
                    {
                        if (operatorStack.Count > 0)
                        {
                            strOperator = operatorStack.Pop();

                        }
                        else
                        {
                            strOperator = string.Empty;
                        }

                        sb.Append(valueQueue.Dequeue());

                        // After each logical operation closing parenthesis 
                        if (previousOp != string.Empty)
                            sb.Append(")");

                        if (strOperator != string.Empty)
                            sb.Append(" " + strOperator + " ");

                        previousOp = strOperator;
                    }
                }
                isSuccess = true;
            }
            catch (Exception)
            {
                isSuccess = false;
            }

            return isSuccess;
        }

        private static bool ProcessRecursiveWhereNode(XmlNode xmlNode, string strOperatorValue, string strOperatorType, ref Stack<string> operatorStack, ref Queue<string> valueQueue)
        {
            bool isSuccess = false;
            string fieldName = string.Empty;
            string value = string.Empty;
            string thisIterationOperatorType = string.Empty;
            string thisIterationOperatorValue = string.Empty;

            try
            {
                XmlNodeList nodeList = xmlNode.ChildNodes;

                //Get Child node - Possible tags {<Or>, <And>, <Eq>, <Neq>, <Gt>, <Lt>, <Geq>, <Leq>, </BeginsWith>, <Contains>, <FieldRef>, <Value>}
                foreach (XmlNode node in nodeList)
                {
                    thisIterationOperatorType = string.Empty;
                    thisIterationOperatorValue = string.Empty;

                    //Check if it's one of these tag: <Or>, <And>, <Eq>, <Neq>, <Gt>, <Lt>, <Geq>, <Leq>, </BeginsWith>, <Contains>
                    thisIterationOperatorValue = GetOperatorString(node.Name, out thisIterationOperatorType);

                    if (thisIterationOperatorType == "statement")
                        operatorStack.Push(thisIterationOperatorValue);

                    //It's one of these tag: <Or>, <And>, <Eq>, <Neq>, <Gt>, <Lt>, <Geq>, <Leq>, </BeginsWith>, <Contains>
                    if (thisIterationOperatorValue != string.Empty)
                    {
                        ProcessRecursiveWhereNode(node, thisIterationOperatorValue, thisIterationOperatorType, ref operatorStack, ref valueQueue);
                    }
                    else //It is probably a <FieldRef> or <Value> tag.
                    {
                        if (node.Name == "FieldRef")
                            fieldName = node.Attributes["Name"].Value.ToString();
                        else if (node.Name == "Value")
                            value = node.LastChild.Value.ToString();
                    }
                }

                if (strOperatorType == "value" && strOperatorValue != string.Empty && fieldName != string.Empty && value != string.Empty)
                {
                    if (strOperatorValue.Contains("LIKE"))
                    {
                        valueQueue.Enqueue(string.Format(strOperatorValue, fieldName, value));
                    }
                    else
                    {
                        valueQueue.Enqueue(string.Format(strOperatorValue, fieldName, "'" + value + "'"));
                    }
                }

                isSuccess = true;
            }
            catch
            {
                isSuccess = false;
                throw;
            }

            return isSuccess;
        }
        static private string GetOperatorString(string tagName, out string operatorType)
        {
            string operatorString = string.Empty;

            switch (tagName)
            {
                case "Or":
                    operatorString = "OR";
                    operatorType = "statement";
                    break;
                case "And":
                    operatorString = "AND";
                    operatorType = "statement";
                    break;
                case "Eq":
                    operatorString = "{0} = {1}";
                    operatorType = "value";
                    break;
                case "Neq":
                    operatorString = "{0} != {1}";
                    operatorType = "value";
                    break;
                case "Gt":
                    operatorString = "{0} > {1}";
                    operatorType = "value";
                    break;
                case "Lt":
                    operatorString = "{0} < {1}";
                    operatorType = "value";
                    break;
                case "Geq":
                    operatorString = "{0} >= {1}";
                    operatorType = "value";
                    break;
                case "Leq":
                    operatorString = "{0} <= {1}";
                    operatorType = "value";
                    break;
                case "BeginsWith":
                    operatorString = "{0} LIKE '{1}%'";
                    operatorType = "value";
                    break;
                case "Contains":
                    operatorString = "{0} LIKE '%{1}%'";
                    operatorType = "value";
                    break;
                default:
                    operatorString = string.Empty;
                    operatorType = string.Empty;
                    break;
            }

            return operatorString;
        }
    }
}
