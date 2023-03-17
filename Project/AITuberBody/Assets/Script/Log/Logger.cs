using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Logger
{
    string outPath;

    List<string> logItems = new List<string>();
    object logItemsLock = new object();

    static Logger logger;

    public Logger( string outPath)
    {
        this.outPath = outPath;
        logger = this;
    }

    static public void Log( string text )
    {
        if (logger == null) return;

        lock (logger.logItemsLock)
        {
            logger.logItems.Add(text);
        }
    }

    public void Update()
    {
        if (logger == null) return;

        lock (logger.logItemsLock)
        {
            if (logger.logItems.Count > 0)
            {
                using (var sr = new System.IO.StreamWriter(logger.outPath, true, System.Text.Encoding.UTF8))
                {
                    foreach (var item in logger.logItems)
                    {
                        var text = $"{System.DateTime.Now.ToString("[yyyy/MM/dd HH:mm:ss]")} {item}";
                        sr.WriteLine(text);
                    }
                }
                logger.logItems.Clear();
            }
        }
    }
}
