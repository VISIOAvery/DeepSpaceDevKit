using System;
using System.Collections;
using System.Collections.Generic;
using TUIO;
using UnityEngine;

public class CreateAndMoveCube : MonoBehaviour
{
    public GameObject CubeReference;
    public TUIOManager TUIOManager;
    Dictionary<TuioContainer, GameObject> TuioToCubeMap = new Dictionary<TuioContainer, GameObject>();

    void Start()
    {
        this.TUIOManager.OnNewContainer += this.onNewContainer;
        this.TUIOManager.OnUpdateContainer += this.onUpdateContainer;
        this.TUIOManager.OnRemoveContainer += this.onRemoveContainer;
        this.CubeReference.SetActive(false);
    }

    private void onRemoveContainer(TuioContainer obj)
    {
        if (this.TuioToCubeMap.ContainsKey(obj))
        {
            GameObject go = this.TuioToCubeMap[obj];
            Destroy(go);
            this.TuioToCubeMap.Remove(obj);
        }
    }

    private void onUpdateContainer(TuioContainer obj)
    {
        if (this.TuioToCubeMap.ContainsKey(obj)) {
            this.TuioToCubeMap[obj].transform.localPosition = new Vector3(obj.X * 10.0f, obj.Y * 10.0f, 0.0f);
        }
    }

    private void onNewContainer(TuioContainer obj)
    {
        Debug.Log("on new");
        GameObject go = GameObject.Instantiate(this.CubeReference, null, true);
        go.transform.localPosition = new Vector3(obj.X * 10.0f, obj.Y * 10.0f, 0.0f);
        go.SetActive(true);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
