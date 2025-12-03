using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class CryptManager : MonoBehaviour
{
    [HideInInspector]
    public string IdFilePath = ".\\id.dat";
    //Lic
    System.DateTime start_time;
    bool ok_LicenseVerify;
    int? lic_exp_days = null;
    bool startTimeCheck;
    private void Awake()
    {
        start_time = System.DateTime.UtcNow;
    }
    void Start()
    {
        StartCoroutine(WriteHardId());
    }
    IEnumerator WriteHardId()
    {
        using (StreamWriter writer = new StreamWriter(IdFilePath, false))
        {
            writer.Write(string.Empty);
        }
        yield return null;
        using (StreamWriter writer = new StreamWriter(IdFilePath, true))
        {
            writer.WriteLine(SnUnityTools.SerialNumberValidateTools.HardwareID);
        }
        StartCoroutine(RuntimeInitCheck());
    }


    IEnumerator RuntimeInitCheck()
    {
        yield return new WaitForEndOfFrame();
        ok_LicenseVerify = SnUnityTools.SerialNumberValidateTools.Verify(out lic_exp_days);
        //lic_exp_days等于null 表示没有时间限制
        if (lic_exp_days != null && lic_exp_days > 0)
        {
            startTimeCheck = true;
        }
        string LicFilePath = Application.streamingAssetsPath + "/lic.ini";
        if (File.Exists(LicFilePath))
        {
            StartCoroutine(CheckVerify());
        }
        else
        {
            Debug.Log("没有找到lic.ini文件");
            Application.Quit();
        }
    }
    IEnumerator CheckVerify()
    {
        // Verify new user license key
        ok_LicenseVerify = SnUnityTools.SerialNumberValidateTools.Verify(out lic_exp_days);
        yield return null;
        if (ok_LicenseVerify)
        {
            Debug.Log("License正常");
        }
        else
        {
            Debug.Log("无权限");//check it!=>ok_LicenseVerify
            Application.Quit();
        }
    }
    void UpdateRuntimeCheck()
    {
        SnUnityTools.SerialNumberValidateTools.UpdateTime();
        if ((System.DateTime.UtcNow - start_time).Days > lic_exp_days)
        {
            Debug.Log("license时长已到期");
            Application.Quit();
        }
        if ((System.DateTime.UtcNow - start_time).TotalMinutes < 1f)
            start_time = System.DateTime.UtcNow;
    }

    void Update()
    {
        if (startTimeCheck)
        {
            UpdateRuntimeCheck();
        }
    }
}
