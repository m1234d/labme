using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Windows.Speech;
using static UnityEngine.ParticleSystem;
using Microsoft.MR;
using Microsoft.MR.LUIS;
using UnityEngine;
using Microsoft.Cognitive.LUIS;
using System.Linq;

public class BurnerController2 : MonoBehaviour
{
    public GameObject scooper;
    public GameObject textObj;
    private ParticleSystem ps;
    private ParticleSystem.ColorOverLifetimeModule co;
    private MinMaxGradient original;
    private string type;
    public ConfidenceLevel confidence = ConfidenceLevel.Medium;
    public float speed = 1;
    KeywordRecognizer recognizer;
    // keyword array
    public string[] Keywords_array;
    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public bool CanHandle(string intentName)
    {
        switch (intentName)
        {
            case "aluminum":
                return true;
            case "copper":
                return true;
            case "bismuth":
                return true;
            case "arsenic":
                return true;
            case "iron":
                return true;
            case "lithium":
                return true;
            case "sodium":
                return true;
            case "nickel":
                return true;
            case "potassium":
                return true;
            case "cesium":
                return true;
            default:
                return false;

        }
    }

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public void Handle(Intent intent, LuisMRResult result)
    {
        switch (intent.Name)
        {
            case "aluminum":
                type = "aluminum";
                break;
            case "copper":
                type = "copper";
                break;
            case "bismuth":
                type = "bismuth";
                break;
            case "arsenic":
                type = "arsenic";
                break;
            case "iron":
                type = "iron";
                break;
            case "lithium":
                type = "lithium";
                break;
            case "sodium":
                type = "sodium";
                break;
            case "nickel":
                type = "nickel";
                break;
            case "potassium":
                type = "potassium";
                break;
            case "cesium":
                type = "cesium";
                break;
        }
    }



    // Start is called before the first frame update
    void Start()
    {
        ps = GetComponent<ParticleSystem>();
        co = ps.colorOverLifetime;
        original = co.color;
        type = "Aluminum";
        // Change size of array for your requirement
        Keywords_array = new string[10];
        Keywords_array[0] = "aluminum";
        Keywords_array[1] = "bismuth";
        Keywords_array[2] = "arsenic";
        Keywords_array[3] = "copper";
        Keywords_array[4] = "iron";
        Keywords_array[5] = "lithium";
        Keywords_array[6] = "sodium";
        Keywords_array[7] = "nickel";
        Keywords_array[8] = "potassium";
        Keywords_array[9] = "cesium";


        // instantiate keyword recognizer, pass keyword array in the constructor
        recognizer = new KeywordRecognizer(Keywords_array, ConfidenceLevel.Medium);
        recognizer.OnPhraseRecognized += OnKeywordsRecognized;
        // start keyword recognizer
        recognizer.Start();

    }
    void OnKeywordsRecognized(PhraseRecognizedEventArgs args)
    {
        type = args.text;
        Debug.Log("Keyword: " + args.text + "; Confidence: " + args.confidence + "; Start Time: " + args.phraseStartTime + "; Duration: " + args.phraseDuration);
        // write your own logic
    }

    public bool Approximately(Vector3 me, Vector3 other, float allowedDifference)
    {
        var dx = me.x - other.x;
        if (Mathf.Abs(dx) > allowedDifference)
            return false;


        var dy = me.y - other.y;

        if (Mathf.Abs(dy) > allowedDifference)
            return false;

        var dz = me.z - other.z;

        return Mathf.Abs(dz) <= allowedDifference;
    }
    // Update is called once per frame
    void Update()
    {
        textObj.GetComponent<TextMesh>().text = type.ToUpper();
        if (Approximately(transform.position, scooper.transform.position, .1f))
        {
            var col = ps.colorOverLifetime;
            if (type == "aluminum")
            {
                col.color = (Color)new Color32(0xC0, 0xC0, 0xC0, 0xFF);
            }
            else if (type == "bismuth")
            {
                col.color = (Color)new Color32(0x00, 0x7F, 0xFF, 0xFF);
            }
            else if (type == "arsenic")
            {
                col.color = (Color)new Color32(0x00, 0x00, 0xFF, 0xFF);
            }
            else if (type == "copper")
            {
                col.color = (Color)new Color32(0x00, 0x80, 0x00, 0xFF);
            }
            else if (type == "iron")
            {
                col.color = (Color)new Color32(0xFF, 0xD7, 0x00, 0xFF);
            }
            else if (type == "lithium")
            {
                col.color = (Color)new Color32(0x99, 0x00, 0x00, 0xFF);
            }
            else if (type == "sodium")
            {
                col.color = (Color)new Color32(0xFF, 0xFF, 0x00, 0xFF);
            }
            else if (type == "nickel")
            {
                col.color = (Color)new Color32(0xFF, 0xFF, 0xFF, 0xFF);
            }
            else if (type == "calcium")
            {
                col.color = (Color)new Color32(0xCB, 0x41, 0x54, 0xFF);
            }
            else if (type == "potassium")
            {
                col.color = (Color)new Color32(0xB6, 0x66, 0xD2, 0xFF);
            }
            else if (type == "cesium")
            {
                col.color = (Color)new Color32(0x8A, 0x2B, 0xE2, 0xFF);
            }
        }
        else
        {
            var col = ps.colorOverLifetime;
            col.color = original;
        }
    }


}
