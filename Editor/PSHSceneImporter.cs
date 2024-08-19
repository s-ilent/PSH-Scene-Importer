using System.Collections.Generic;
using System;
using System.IO;
using System.Xml;
using System.Text;
using UnityEngine;
using UnityEditor;
using Unity.Collections;
using UnityEditor.AssetImporters;
using Object = UnityEngine.Object;

using Vector3 = UnityEngine.Vector3;
using Vector4 = UnityEngine.Vector4;

namespace SilentTools
{

public static class XmlNodeExtensions
{
    public static XmlNode SelectDirectChildNode(this XmlNode node, string localName)
    {
        return node.SelectSingleNode($"*[local-name()='{localName}']");
    }
}

[CustomEditor(typeof(PSHSceneImporter))]
[CanEditMultipleObjects]
public class PSHSceneImporterEditor : UnityEditor.AssetImporters.ScriptedImporterEditor
{
}

[UnityEditor.AssetImporters.ScriptedImporter(1, "scene")]
public class PSHSceneImporter : UnityEditor.AssetImporters.ScriptedImporter
{
    public float m_Scale = 1.00f;
    public string[] assetSearchPath = new []
    {"",};

	private AssetImportContext currentCtx;
    [SerializeField]
    private XmlDocument xmlDoc;
    [SerializeField]
    private XmlNode gameNode;
    [SerializeField]
    private XmlNamespaceManager nsmgr;



    // Debugging stuff
    private void DbgNote(string note)
    {
        #if true
        Debug.Log(note);
        #endif
    }

    private string DisplayAllNodes(XmlNode node, string indent = "")
    {
        StringBuilder sb = new StringBuilder();

        sb.AppendLine($"{indent}Node: {node.Name}, Namespace: {node.NamespaceURI}");

        if (node.Attributes != null)
        {
            foreach (XmlAttribute attr in node.Attributes)
            {
                sb.AppendLine($"{indent}  Attribute: {attr.Name}, Value: {attr.Value}");
            }
        }

        foreach (XmlNode child in node.ChildNodes)
        {
            sb.Append(DisplayAllNodes(child, indent + "  "));
        }

        return sb.ToString();
    }

private void DbgPrintNodeProperties(XmlNode node)
{
    StringBuilder sb = new StringBuilder();
    if (node == null)
    {
        DbgNote("Node is null");
        return;
    }

    sb.AppendLine($"Node Name: {node.Name}");
    sb.AppendLine($"Node Local Name: {node.LocalName}");
    sb.AppendLine($"Node Namespace URI: {node.NamespaceURI}");

    if (node.Attributes != null)
    {
        foreach (XmlAttribute attr in node.Attributes)
        {
            sb.AppendLine($"Attribute: {attr.Name}, Value: {attr.Value}, Namespace URI: {attr.NamespaceURI}");
        }
    }

    if (node.HasChildNodes)
    {
        sb.AppendLine($"Node has {node.ChildNodes.Count} child nodes");
    }
    else
    {
        sb.AppendLine("Node has no child nodes");
    }
    DbgNote(sb.ToString());
}

    // https://forum.unity.com/threads/right-hand-to-left-handed-conversions.80679/
    public static Quaternion ConvertMayaRotationToUnity(Vector3 rotation) {
        Vector3 flippedRotation = new Vector3(rotation.x, -rotation.y, -rotation.z); // flip Y and Z axis for right->left handed conversion
        // convert XYZ to ZYX
        Quaternion qx = Quaternion.AngleAxis(flippedRotation.x, Vector3.right);
        Quaternion qy = Quaternion.AngleAxis(flippedRotation.y, Vector3.up);
        Quaternion qz = Quaternion.AngleAxis(flippedRotation.z, Vector3.forward);
        Quaternion qq = qz * qy * qx; // this is the order
        return qq;
    }

    public PSHSceneImporter(string xmlData)
    {
        DbgNote("Init: Starting PSHSceneImporter");

        xmlDoc = new XmlDocument();
        xmlDoc.LoadXml(xmlData);
        DbgNote($"LoadXml: Loaded XML data into XmlDocument instance. xmlDoc.InnerXml.Length = {xmlDoc.InnerXml.Length}");

        nsmgr = new XmlNamespaceManager(xmlDoc.NameTable);
        //nsmgr.AddNamespace("gap", "http://home.scedev.net/schema/sceneeditor/1_3_0");
        nsmgr.AddNamespace("gap", "http://home.scedev.net/schema/sceneeditor/1_3_0");

        DbgNote("Init: Finished PSHSceneImporter");
    }


    public void LoadGameObjects(GameObject rootGO)
    {
        XmlNodeList allNodes = xmlDoc.SelectNodes("//*", nsmgr);
        XmlNode game = xmlDoc.SelectDirectChildNode("game");
        XmlNode goFolder = game.SelectDirectChildNode("gameObjectFolder");

        if (goFolder == null)
        {
            DbgNote("LoadGameObjects: goFolder is null");
        }
        else
        {
            DbgNote($"LoadGameObjects: Selected 'gameObjectFolder' node from xmlDoc. game.InnerXml.Length = {goFolder.InnerXml.Length}");
            // Todo: It would be useful to have a report of assetFolder/assets and Prototypes, Layers if they exist
            LoadNestedObjects(goFolder, rootGO);
        }

        DbgNote("LoadGameObjects: Finished LoadGameObjects");
    }

    private void LoadNestedObjects(XmlNode parentNode, GameObject parentGO)
    {
        foreach (XmlNode node in parentNode.ChildNodes)
        {
            GameObject go = parentGO;

            // All GameObjects have an xsi:type
            if (node.Attributes != null && node.Attributes["xsi:type"] != null)
            {
                go = new GameObject(node.Attributes["name"].Value);
                go.transform.parent = parentGO?.transform;
                LoadTransform(go, node);
                LoadComponent(go, node);
            }

            if (node.Attributes != null && node.Name == "folder")
            {
                go = new GameObject(node.Attributes["name"].Value);
                go.transform.parent = parentGO?.transform;
            }

            if (node.Attributes != null && node.Name == "seat")
            {
                go = new GameObject(node.Attributes["name"].Value);
                go.transform.parent = parentGO?.transform;
                LoadTransform(go, node);
                LoadComponent(go, node);
            }

            // child of xsi:type="soundZone2Type" 
            if (node.Attributes != null && node.Name == "subzone")
            {
                go = new GameObject(node.Attributes["name"].Value);
                go.transform.parent = parentGO?.transform;
                LoadTransform(go, node);
            }
            // child of xsi:type="cubePointSoundType" 
            if (node.Attributes != null && node.Name == "emitter")
            {
                go = new GameObject(node.Attributes["name"].Value);
                go.transform.parent = parentGO?.transform;
                LoadTransform(go, node);
                LoadComponent(go, node);
            }
            

            // Recursively load nested objects
            if (node.HasChildNodes)
            {
                LoadNestedObjects(node, go);
            }
        }
    }

    private void LoadTransform(GameObject go, XmlNode node)
    {
        XmlNode transformNode = node.SelectDirectChildNode("transform");
        XmlNode translateNode = node.SelectDirectChildNode("translate");
        XmlNode rotateNode = node.SelectDirectChildNode("rotate");
        XmlNode scaleNode = node.SelectDirectChildNode("scale");

        if (transformNode != null)
        {
            string[] values = transformNode.InnerText.Split(' ');
            Matrix4x4 matrix = new Matrix4x4(
                new Vector4(float.Parse(values[0]), float.Parse(values[1]), float.Parse(values[2]), float.Parse(values[3])),
                new Vector4(float.Parse(values[4]), float.Parse(values[5]), float.Parse(values[6]), float.Parse(values[7])),
                new Vector4(float.Parse(values[8]), float.Parse(values[9]), float.Parse(values[10]), float.Parse(values[11])),
                new Vector4(float.Parse(values[12]), float.Parse(values[13]), float.Parse(values[14]), float.Parse(values[15]))
            );
            Vector3 translate = matrix.MultiplyPoint3x4(Vector3.zero);
            translate.x *= -1.0f;
            go.transform.localPosition = translate;
            go.transform.localRotation = matrix.rotation;
            go.transform.localScale = matrix.lossyScale;
        }

        if (translateNode != null)
        {
            string[] values = translateNode.InnerText.Split(' ');
            Vector3 translate = new Vector3(float.Parse(values[0]), float.Parse(values[1]), float.Parse(values[2]));
            translate.x *= -1.0f;
            go.transform.localPosition = translate;
        }

        if (rotateNode != null)
        {
            string[] values = rotateNode.InnerText.Split(' ');
            Vector3 rotate = new Vector3(float.Parse(values[0]), float.Parse(values[1]), float.Parse(values[2]));
            go.transform.rotation = ConvertMayaRotationToUnity(rotate * Mathf.Rad2Deg);
        }

        if (scaleNode != null)
        {
            string[] values = scaleNode.InnerText.Split(' ');
            Vector3 scale = new Vector3(float.Parse(values[0]), float.Parse(values[1]), float.Parse(values[2]));
            go.transform.localScale = scale;
        }

    }

    private void LoadComponent(GameObject go, XmlNode node)
    {
        // Add a new component to the GameObject and populate it with data from the XML
        XMLDataComponent dataComponent = go.AddComponent<XMLDataComponent>();
        foreach (XmlAttribute attribute in node.Attributes)
        {
            SerializableKeyValuePair pair;
            pair.key = attribute.Name;
            pair.value = attribute.Value;
            dataComponent.data.Add(pair);
        }
        
        foreach (XmlNode subNode in node.ChildNodes)
        {
            // If the subNode only has attributes and no child nodes, add a new XMLDataComponent
            if (subNode.Attributes != null && subNode.ChildNodes.Count == 0)
            {
                XMLDataComponent subDataComponent = go.AddComponent<XMLDataComponent>();
                foreach (XmlAttribute attribute in subNode.Attributes)
                {
                    SerializableKeyValuePair pair;
                    pair.key = attribute.Name;
                    pair.value = attribute.Value;
                    subDataComponent.data.Add(pair);
                }
            }
        }
    }


    public override void OnImportAsset(AssetImportContext ctx)
    {
		// Load using a FileStream
        string shortName = Path.GetFileNameWithoutExtension(ctx.assetPath);

		// Set the root object to a proxy GO to make things a bit easier.
        var rootGO = new GameObject(shortName);
        rootGO.transform.localScale = new Vector3(1.0f, 1.0f, 1.0f) * m_Scale;
        
        // Load the XML data
        string xmlData = File.ReadAllText(ctx.assetPath);
        PSHSceneImporter xmlLoader = new PSHSceneImporter(xmlData);

        // Parse the XML and load the GameObjects
        xmlLoader.LoadGameObjects(rootGO);

        ctx.AddObjectToAsset("Root", rootGO);
        ctx.SetMainObject(rootGO);
	}
}

}