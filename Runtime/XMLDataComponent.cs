using System.Collections.Generic;
using System;
using System.IO;
using System.Xml;
using System.Text;
using UnityEngine;
using Unity.Collections;
using Object = UnityEngine.Object;

using Vector3 = UnityEngine.Vector3;
using Vector4 = UnityEngine.Vector4;
namespace SilentTools
{
    
[System.Serializable]
public struct SerializableKeyValuePair
{
    public string key;
    public string value;
}

public class XMLDataComponent : MonoBehaviour
{

    public List<SerializableKeyValuePair> data = new List<SerializableKeyValuePair>();
}
}