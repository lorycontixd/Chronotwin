using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestRecordingBall : MonoBehaviour
{
    #region Singleton
    private static TestRecordingBall _instance;
    public static TestRecordingBall Instance { get { return _instance; } }


    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            _instance = this;
        }
    }
    #endregion


    private void Start()
    {
    }

}   
