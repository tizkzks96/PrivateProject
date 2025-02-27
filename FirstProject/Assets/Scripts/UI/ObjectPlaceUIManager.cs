﻿using GoogleARCore.Examples.HelloAR;
using GoogleARCore.Examples.ObjectManipulation;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public enum PlaceUIState
{
    NONE,
    HOME,
    ENVIRONMENT,
    BUILDING,
    PREVIEW
}

public class ObjectPlaceUIManager : MonoBehaviour
{
    public static ObjectPlaceUIManager instance;

    public GameObject placePlanePrefab;

    public int maxSlotCount = 4;

    [HideInInspector]
    public GameObject placePlane;

    public GameObject buildingSlot;
    public GameObject emptyBuildingSlot;

    [Header("Menu")]
    public GameObject placeUI;
    public GameObject mainUI;
    public GameObject evironmentUI;
    public GameObject buildingUI;
    public GameObject previewUI;

    [Space(10)]


    public GameObject canvas;

    public GameObject temp;

    public static int BUILDING_SLOT_COUNT = 0;

    private GameObject m_currentMenu;

    public GameObject spotSquare;

    public PlaceUIState placeUIState;
    public GameObject CurrentMenu { get => m_currentMenu;}

    public Sprite buildingSprite;

    public void Awake()
    {
        //Check if instance already exists
        if (instance == null)
        {
            InitPlace();
            //if not, set instance to this
            instance = this;

        }


        //If instance already exists and it's not this:
        else if (instance != this)

            //Then destroy this. This enforces our singleton pattern, meaning there can only ever be one instance of a GameManager.
            Destroy(gameObject);
    }

    public void InitPlace()
    {
        if (placePlane == null)
        {
            //data class
            placePlane = Instantiate(placePlanePrefab);

            canvas = placePlane.transform.Find("Canvas").gameObject;

            //UI Link
            mainUI = placeUI.transform.Find("MainUI").gameObject;

            evironmentUI = placeUI.transform.Find("EnvironmentUI").gameObject;

            buildingUI = placeUI.transform.Find("BuildingUI").gameObject;

            previewUI = placeUI.transform.Find("PreviewUI").gameObject;

            spotSquare = canvas.transform.Find("spotSquare").gameObject;

            #region UIState 이동 버튼
            //HomePanel 버튼 연결
            mainUI.transform.Find("Exit").GetComponent<Button>().onClick.AddListener(() => { ChangeUIState(PlaceUIState.NONE); });
            mainUI.transform.Find("Building").GetComponent<Button>().onClick.AddListener(() => { ChangeUIState(PlaceUIState.ENVIRONMENT); });
            mainUI.transform.Find("Enviroment").GetComponent<Button>().onClick.AddListener(() => { ChangeUIState(PlaceUIState.BUILDING); });

            //EnvironmentPanel 버튼 연결
            evironmentUI.transform.Find("Home").GetComponent<Button>().onClick.AddListener(() => { ChangeUIState(PlaceUIState.HOME); });

            //BuildingPanel 버튼 연결
            buildingUI.transform.Find("Home").GetComponent<Button>().onClick.AddListener(() => { ChangeUIState(PlaceUIState.HOME); });

            buildingUI.transform.Find("Add").GetComponent<Button>().onClick.AddListener(() => { InstantiateEmptyBuildingSlot(); });

            #endregion

            m_currentMenu = mainUI;

            for(int i = 1; i< evironmentUI.transform.childCount; i++)
            {
                Transform button = evironmentUI.transform.GetChild(i);
                Outline outline = button.gameObject.AddComponent<Outline>();
                outline.OutlineColor =  new Color(255, 255, 255);
                outline.OutlineWidth = 2;
                button.GetComponent<Button>().onClick.AddListener(() => {
                    //button.GetComponent<SlotInfo>().slotinfo.BuildingPrefab.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
                    HelloARController.instance.PlaceObject(placePlane, button.GetComponent<SlotInfo>().slotinfo.BuildingPrefab, true);
                    ChangeUIState(PlaceUIState.NONE);
                });
            }

            //InstantiateBuildingSlot(BuildingDatabase.Instance.GetByID(0));
            //InstantiateBuildingSlot(1);
            //InstantiateBuildingSlot(2);

            //캡슐화   
            mainUI.SetActive(false);
            evironmentUI.SetActive(false);
            buildingUI.SetActive(false);
            previewUI.SetActive(false);
            spotSquare.SetActive(false);
        }
    }


 

    public void StartPlace(TapGesture gesture)
    {
        if(spotSquare.activeSelf == true)
        {
            return;
        }

        if (spotSquare.activeSelf == false)
        {
            m_currentMenu.SetActive(true);
        }
        placePlane.transform.SetParent(gesture.TargetObject.transform);
        HelloARController.instance.TapGesturePositionCorrection(gesture, placePlane.transform, 0.01f);
        placePlane.transform.localRotation = Quaternion.Euler(0,0,0);

        SetButtonPosition(0.5f);
        ChangeUIState(PlaceUIState.HOME);
    }

    public void SetButtonPosition(float radius)
    {
        if(CurrentMenu == null)
        {
            return;
        }

        int amount = CurrentMenu.transform.childCount;
        print("CurrnetMenu : " + CurrentMenu.transform + " : " + CurrentMenu.transform.childCount);
        for (int i = 0; i < amount; i++)
        {
            GameObject currnetOjbect = CurrentMenu.transform.GetChild(i).gameObject;
            float cornerAngle = 2f * Mathf.PI / (float)amount * i;
            currnetOjbect.transform.localPosition = new Vector2(Mathf.Cos(cornerAngle) * radius,  Mathf.Sin(cornerAngle) * radius);
        }
    }

    public void MakeSlotPosition(float radius, int amount)
    {
        for (int i = 0; i < amount; i++)
        {
            float cornerAngle = 2f * Mathf.PI / (float)amount * i;
            GameObject slotTemp = Instantiate(temp);
            slotTemp.transform.SetParent(placePlane.transform);
            slotTemp.transform.localPosition = new Vector3(Mathf.Cos(cornerAngle) * radius, 0.01f, Mathf.Sin(cornerAngle) * radius);
        }
    }

    public void InstantiateEmptyBuildingSlot()
    {
        if (BUILDING_SLOT_COUNT >= maxSlotCount)
        {
            Destroy(buildingUI.transform.Find("Add").gameObject);
            SetButtonPosition(0.5f);
        }

        GameObject slot = Instantiate(emptyBuildingSlot, buildingUI.transform);

        slot.GetComponent<Button>().onClick.AddListener(() => {
            SceanContorller.instance.Editing = slot;
            SceanContorller.instance.ChangeScean(SceanState.AUGMENTEDIMAGE);
        });

        slot.GetComponent<SlotInfo>().Slotinfo.ID = BUILDING_SLOT_COUNT;

        SetButtonPosition(0.5f);

        BUILDING_SLOT_COUNT++;
    }

    /// <summary>
    /// 빌딩 슬롯 생성
    /// </summary>
    /// <param name="buildingInfo"></param>
    /// <param name="mat">빌딩에 할당할 머티리얼</param>
    public void InstantiateBuildingSlot(BuildingInfo buildingInfo, Material mat)
    {
        //슬롯생성
        GameObject slot = SceanContorller.instance.Editing;

        //슬롯정보
        BuildingInfo slotInfo = slot.GetComponent<SlotInfo>().Slotinfo;

        //슬롯에 넣을 빌딩 생성
        GameObject building = Instantiate(buildingInfo.BuildingPrefab);

        //빌딩 리사이즈
        building.transform.localScale = new Vector3(0.006f, 0.006f, 0.006f);

        //building.SetActive(false);

        //슬롯정보에 머티리얼 할당
        if (mat != null)
        {
            building.GetComponent<Renderer>().material = mat;
        }

        //슬롯에 빌딩 할당
        slotInfo.BuildingPrefab = building;

        //슬롯에 이미지 할당
        if(buildingInfo.Texture2d != null)
        {
            Sprite slotSprite = slot.transform.GetChild(0).GetComponent<Image>().sprite;
            slot.transform.GetChild(0).GetComponent<Image>().sprite = Sprite.Create(buildingInfo.Texture2d, new Rect(0, 0, buildingInfo.Texture2d.width, buildingInfo.Texture2d.height), Vector2.zero);
        }

        //버튼 이벤트 삭제
        slot.GetComponent<Button>().onClick.RemoveAllListeners();
            
        //버튼 이벤트 할당
        slot.GetComponent<Button>().onClick.AddListener(() => {
            HelloARController.instance.PlaceObject(placePlane, slotInfo.BuildingPrefab);
            ChangeUIState(PlaceUIState.NONE);
        });

        //버튼 재배치
        SetButtonPosition(0.5f);

        //빌딩 오브젝트를 삭제하면 레퍼런스를 잃음
        //프리펩을 파일로 저장하고 리소스로 링크하여 해결하도록함
        //building 프리펩을 카메라에 고정하여 회전하도록 함
        //building.SetActive(false);
        building.transform.SetParent(FindObjectOfType<Camera>().transform);
        building.transform.localPosition = new Vector3(0, -0.04f, 0.2f);
        building.transform.localRotation = Quaternion.Euler(-40, -1, 0);
        StartCoroutine(CustomAnimationCurve.Instance.RotationTargetAnimation(building, 10));
        if (building.GetComponent<Outline>() != null)
            building.GetComponent<Outline>().enabled = false;
        mainUI.SetActive(false);

        ChangeUIState(PlaceUIState.PREVIEW);
        previewUI.transform.Find("OK").GetComponent<Button>().onClick.RemoveAllListeners();
        previewUI.transform.Find("OK").GetComponent<Button>().onClick.AddListener(() => {
            mainUI.SetActive(true);
            ChangeUIState(PlaceUIState.BUILDING);
            building.SetActive(false);

            //PlayAnimation(previewUI.GetComponent<Animation>(), "ScaleDown");
            //StartCoroutine(CustomAnimationCurve.Instance.ScaleDownTargetAnimation(building, 10, 10));
        });

    }



    public void PlayAnimation(Animation anim, string clipName)
    {
        anim.clip = anim.GetClip(clipName);
        anim.Play();
    }

    public void ChangeUIState(PlaceUIState uiState)
    {
        switch (uiState)
        {
            case PlaceUIState.NONE:
                placeUIState = PlaceUIState.NONE;

                //캡슐화
                PlayAnimation(m_currentMenu.GetComponent<Animation>(), "ScaleDown");

                spotSquare.SetActive(false);

                m_currentMenu = mainUI;
                print("qweqwe : " + m_currentMenu);

                break;
            case PlaceUIState.HOME:
                placeUIState = PlaceUIState.HOME;

                m_currentMenu = mainUI;

                SetButtonPosition(0.5f);

                spotSquare.SetActive(true);

                mainUI.SetActive(true);

                evironmentUI.SetActive(false);

                buildingUI.SetActive(false);

                previewUI.SetActive(false);

                PlayAnimation(m_currentMenu.GetComponent<Animation>(), "ScaleUp");

                break;
            case PlaceUIState.ENVIRONMENT:
                placeUIState = PlaceUIState.ENVIRONMENT;

                m_currentMenu = evironmentUI;

                SetButtonPosition(0.5f);

                spotSquare.SetActive(true);

                mainUI.SetActive(false);

                evironmentUI.SetActive(true);

                buildingUI.SetActive(false);

                previewUI.SetActive(false);

                PlayAnimation(m_currentMenu.GetComponent<Animation>(), "ScaleUp");

                break;
            case PlaceUIState.BUILDING:
                placeUIState = PlaceUIState.BUILDING;

                m_currentMenu = buildingUI;

                SetButtonPosition(0.5f);

                spotSquare.SetActive(true);

                mainUI.SetActive(false);

                evironmentUI.SetActive(false);

                buildingUI.SetActive(true);

                previewUI.SetActive(false);

                PlayAnimation(m_currentMenu.GetComponent<Animation>(), "ScaleUp");

                break;
            case PlaceUIState.PREVIEW:
                placeUIState = PlaceUIState.PREVIEW;

                m_currentMenu = previewUI;

                spotSquare.SetActive(false);

                mainUI.SetActive(false);

                evironmentUI.SetActive(false);

                buildingUI.SetActive(false);

                previewUI.SetActive(true);


                //PlayAnimation(m_currentMenu.GetComponent<Animation>(), "ScaleUp");

                break;
            default:
                break;
        }
    }
}
