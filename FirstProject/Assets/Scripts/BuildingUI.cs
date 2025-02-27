﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using GoogleARCore.Examples.HelloAR;
using GoogleARCore;
using GoogleARCoreInternal;
using System.IO;



/// <summary>
/// 
/// 
/// 
/// 나중에 여기에 BuildingSlot 추가할 때 디비에서 가져온걸 for문 돌려서 추가해야함
/// </summary>
public class BuildingUI : MonoBehaviour
{
    public static BuildingUI instance;

    public GameObject HomePanel;

    public GameObject EnvironmentPanel;

    public GameObject BuildingPanel;

    public GameObject buildingSlot;

    private Canvas canvas;


    public void Awake()
    {
        //Check if instance already exists
        if (instance == null)
        {
            canvas = FindObjectOfType<Canvas>();


            //if not, set instance to this
            instance = this;
        }


        //If instance already exists and it's not this:
        else if (instance != this)

            //Then destroy this. This enforces our singleton pattern, meaning there can only ever be one instance of a GameManager.
            Destroy(gameObject);
    }

    // Start is called before the first frame update
    void Start()
    {
        #region UIState 이동 버튼
        //HomePanel 버튼 연결
        HomePanel.transform.Find("EnvironmentBtn").GetComponent<Button>().onClick.AddListener(() => { ChangeState(PlaceUIState.ENVIRONMENT); });
        HomePanel.transform.Find("BuildingsBtn").GetComponent<Button>().onClick.AddListener(() => { ChangeState(PlaceUIState.BUILDING); });

        //EnvironmentPanel 버튼 연결
        EnvironmentPanel.transform.Find("Home").GetComponent<Button>().onClick.AddListener(() => { ChangeState(PlaceUIState.HOME); });

        //BuildingPanel 버튼 연결
        BuildingPanel.transform.Find("Home").GetComponent<Button>().onClick.AddListener(() => { ChangeState(PlaceUIState.HOME); });

        BuildingPanel.transform.Find("Add").GetComponent<Button>().onClick.AddListener(() => { SceanContorller.instance.ChangeScean(SceanState.AUGMENTEDIMAGE); });
        #endregion



        //기본 빌딩슬롯 생성
        InstantiateBuildingSlot(0);
        InstantiateBuildingSlot(1);
        InstantiateBuildingSlot(2);

    }

    /// <summary>
    /// UI Panel 상태를 체인지한다.
    /// </summary>
    public void ChangeState(PlaceUIState uiState)
    {
        switch (uiState)
        {
            case PlaceUIState.HOME:
                HomePanel.SetActive(true);

                EnvironmentPanel.SetActive(false);

                BuildingPanel.SetActive(false);
                break;
            case PlaceUIState.ENVIRONMENT:
                HomePanel.SetActive(false);

                EnvironmentPanel.SetActive(true);

                BuildingPanel.SetActive(false);
                break;
            case PlaceUIState.BUILDING:
                HomePanel.SetActive(false);

                EnvironmentPanel.SetActive(false);

                BuildingPanel.SetActive(true);
                break;
            default:
                break;
        }
    }

    /// <summary>
    /// BuildingSlot object 를 생성한다.
    /// </summary>
    /// <param name="index">BuildingDatabase에 저장된 index</param>
    public void InstantiateBuildingSlot(int index, Texture2D texture = null, Transform parant = null)
    {
        GameObject slot;

        if (canvas == null)
            return;

        //slot 생성
        if(parant == null)
        {
            slot = Instantiate(buildingSlot, EnvironmentPanel.transform);
        }
        else
        {
            slot = Instantiate(buildingSlot, parant);
        }

        BuildingInfo slotInfo = BuildingDatabase.Instance.GetByID(index);

        if (texture != null)
        {
            slotInfo.BuildingPrefab.GetComponent<Renderer>().sharedMaterial.mainTexture = texture;

        }

        slotInfo.BuildingPrefab.transform.localScale = new Vector3(0.006f, 0.006f, 0.006f);

        slot.GetComponent<SlotInfo>().Slotinfo = slotInfo;

        //slot.transform.GetChild(0).GetComponent<Text>().text = "made : " + slotInfo.Name ;
    }
}
