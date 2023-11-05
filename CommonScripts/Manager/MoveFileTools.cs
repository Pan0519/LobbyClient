using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using System.IO;
using System;

public class MoveFileTools : MonoSingleton<MoveFileTools>
{
    public void moveFile(string originalPath, string targetPath, Action callbackFunc = null, params string[] fileName)
    {
        StartCoroutine(addMoveFileCoroutine(originalPath, targetPath, callbackFunc, fileName));
    }
    
    private IEnumerator addMoveFileCoroutine(string originalPath, string targetPath, Action callbackFunc = null, params string[] fileName)
    {
        for (var i = 0; i < fileName.Length; i++)
        {
            yield return StartCoroutine(getFile(originalPath, targetPath, fileName[i]));

            if (i == fileName.Length - 1)
            {
                callbackFunc();
            }
        }
    }

    private IEnumerator getFile(string originalPath, string targetPath, string fileName)
    {
        string filePath = Path.Combine(originalPath, fileName);
        using (UnityWebRequest loadingRequest = UnityWebRequest.Get(filePath))
        {
            yield return loadingRequest.SendWebRequest();

            if (loadingRequest.result == UnityWebRequest.Result.Success && loadingRequest.downloadHandler.data != null)
            {
                Debug.Log("Get File Success : " + fileName);
                copyFile(targetPath, fileName, loadingRequest.downloadHandler.data);
            }
        }
    }

    private void copyFile(string targetPath, string fileName, byte[] fileData)
    {
        string filePath = Path.Combine(targetPath, fileName);
        File.WriteAllBytes(filePath, fileData);

        if (File.Exists(filePath))
        {
            Debug.Log("Copy File Success : " + fileName);
        }
        else
        {
            Debug.LogError("Copy File Fail, Path : " + filePath);
        }
    }
}
