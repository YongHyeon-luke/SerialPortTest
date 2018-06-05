using UnityEngine;
using System.IO;

public class ConfigFile : MonoBehaviour {

    public SerialSendReceive serialSendReceive;
    private string currentDirectory;
    private char delimiter = ':';
    private string readLine;
    private string[] lines;

    void Awake()
    {
        currentDirectory = Directory.GetCurrentDirectory();
        if (!File.Exists(currentDirectory + @"\Comport.CFG"))
        {
            using (StreamWriter wr = new StreamWriter(currentDirectory + @"\Comport.CFG"))
            {
                wr.WriteLine("COMPORT:" + serialSendReceive.currentSerialPortName);
            }
        }
    }

    // Use this for initialization
    void OnEnable()
    {
        using (StreamReader rdr = new StreamReader(currentDirectory + @"\Comport.CFG"))
        {
            while ((readLine = rdr.ReadLine()) != null)
            {
                lines = readLine.Split(delimiter);
            }
        }

        serialSendReceive.currentSerialPortName = lines[1];
    }
}
