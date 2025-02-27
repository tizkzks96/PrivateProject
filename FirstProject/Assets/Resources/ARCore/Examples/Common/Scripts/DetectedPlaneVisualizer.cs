//-----------------------------------------------------------------------
// <copyright file="DetectedPlaneVisualizer.cs" company="Google">
//
// Copyright 2017 Google Inc. All Rights Reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
// </copyright>
//-----------------------------------------------------------------------

namespace GoogleARCore.Examples.Common
{
    using System.Collections;
    using System.Collections.Generic;
    using GoogleARCore;
    using GoogleARCore.Examples.ObjectManipulation;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// Visualizes a single DetectedPlane in the Unity scene.
    /// </summary>
    public class DetectedPlaneVisualizer : MonoBehaviour
    {
        public Text prograssText;

        private DetectedPlane m_DetectedPlane;

        // Keep previous frame's mesh polygon to avoid mesh update every frame.
        private List<Vector3> m_PreviousFrameMeshVertices = new List<Vector3>();
        private List<Vector3> m_MeshVertices = new List<Vector3>();
        private Vector3 m_PlaneCenter = new Vector3();

        private List<Color> m_MeshColors = new List<Color>();

        private List<int> m_MeshIndices = new List<int>();

        public static Anchor cubeWorldAnchor;

        private Mesh m_Mesh;

        public MeshRenderer m_MeshRenderer;
        public GameObject GroundManipulatorPrefab;

        public Camera firstPersonCamera;

        public static GameObject cubeWorld;

        private bool _endDetect = true;

        float minDistance;

        private static Vector3 gab;


        public List<Vector3> MeshVertices { get => m_MeshVertices; set => m_MeshVertices = value; }
        public Vector3 PlaneCenter { get => m_PlaneCenter; set => m_PlaneCenter = value; }
        public static Vector3[,] MapArray { get; set; } = new Vector3[50, 50];
        public static Vector3 Gab { get => MapArray[0, 0]; set => gab = value; }


        
        /// <summary>
        /// The Unity Awake() method.
        /// </summary>
        public void Awake()
        {

            firstPersonCamera = FindObjectOfType<Camera>();
            m_Mesh = GetComponent<MeshFilter>().mesh;
            m_MeshRenderer = GetComponent<UnityEngine.MeshRenderer>();
        }

        /// <summary>
        /// The Unity Update() method.
        /// </summary>
        public void Update()
        {
            if (cubeWorld != null)
            {
                if(m_DetectedPlane.TrackingState == TrackingState.Stopped)
                {
                    cubeWorldAnchor.transform.SetParent(null);
                    cubeWorld.SetActive(false);
                    DetectedPlaneGenerator.Instance.EnableCtrl(true);
                }
                if (SceanContorller.instance.SceanState == SceanState.MAIN && m_DetectedPlane.TrackingState == TrackingState.Tracking)
                {
                    cubeWorld.SetActive(true);
                    cubeWorldAnchor.transform.SetParent(transform);
                }

                m_MeshRenderer.enabled = false;
                return;
            }


                if (m_DetectedPlane == null)
            {
                return;
            }
            else if (m_DetectedPlane.SubsumedBy != null)
            {
                Destroy(gameObject);
                return;
            }
            else if (m_DetectedPlane.TrackingState != TrackingState.Tracking)
            {
                m_MeshRenderer.enabled = false;
                if (cubeWorld == null)
                {
                    DetectedPlaneGenerator.Instance.EnableCtrl(true);
                    prograssText.text = "카메라를 바닥을 향해 천천히 움직여 보세요";
                    Destroy(gameObject);
                }
                return;
            }

            //m_MeshRenderer.enabled = true;


            _UpdateMeshIfNeeded();
            //if(cubeWorld != null)
            //{
            SelectArea();
            //print("polyfonArea : " + polygonArea());
            //}
        }



        /// <summary>
        /// Initializes the DetectedPlaneVisualizer with a DetectedPlane.
        /// </summary>
        /// <param name="plane">The plane to vizualize.</param>
        public void Initialize(DetectedPlane plane)
        {
            m_DetectedPlane = plane;
            //m_MeshRenderer.material.SetColor("_GridColor", Color.white);
            m_MeshRenderer.material.SetFloat("_UvRotation", Random.Range(0.0f, 360.0f));

            Update();
        }

        /// <summary>
        /// Update mesh with a list of Vector3 and plane's center position.
        /// </summary>
        private void _UpdateMeshIfNeeded()
        {
            m_DetectedPlane.GetBoundaryPolygon(MeshVertices);

            if (_AreVerticesListsEqual(m_PreviousFrameMeshVertices, MeshVertices))
            {
                return;
            }

            m_PreviousFrameMeshVertices.Clear();
            m_PreviousFrameMeshVertices.AddRange(MeshVertices);

            PlaneCenter = m_DetectedPlane.CenterPose.position;

            Vector3 planeNormal = m_DetectedPlane.CenterPose.rotation * Vector3.up;

            m_MeshRenderer.material.SetVector("_PlaneNormal", planeNormal);

            int planePolygonCount = MeshVertices.Count;

            // The following code converts a polygon to a mesh with two polygons, inner polygon
            // renders with 100% opacity and fade out to outter polygon with opacity 0%, as shown
            // below.  The indices shown in the diagram are used in comments below.
            // _______________     0_______________1
            // |             |      |4___________5|
            // |             |      | |         | |
            // |             | =>   | |         | |
            // |             |      | |         | |
            // |             |      |7-----------6|
            // ---------------     3---------------2
            //m_MeshColors.Clear();

            // Fill transparent color to vertices 0 to 3.
            for (int i = 0; i < planePolygonCount; ++i)
            {
                //m_MeshColors.Add(Color.clear);
            }

            // Feather distance 0.2 meters.
            const float featherLength = 0.2f;

            // Feather scale over the distance between plane center and vertices.
            const float featherScale = 0.2f;

            // Add vertex 4 to 7.
            for (int i = 0; i < planePolygonCount; ++i)
            {
                Vector3 v = MeshVertices[i];

                // Vector from plane center to current point
                Vector3 d = v - PlaneCenter;

                float scale = 1.0f - Mathf.Min(featherLength / d.magnitude, featherScale);
                MeshVertices.Add((scale * d) + PlaneCenter);

                //m_MeshColors.Add(Color.white);
            }

            m_MeshIndices.Clear();
            int firstOuterVertex = 0;
            int firstInnerVertex = planePolygonCount;

            // Generate triangle (4, 5, 6) and (4, 6, 7).
            for (int i = 0; i < planePolygonCount - 2; ++i)
            {
                m_MeshIndices.Add(firstInnerVertex);
                m_MeshIndices.Add(firstInnerVertex + i + 1);
                m_MeshIndices.Add(firstInnerVertex + i + 2);
            }

            // Generate triangle (0, 1, 4), (4, 1, 5), (5, 1, 2), (5, 2, 6), (6, 2, 3), (6, 3, 7)
            // (7, 3, 0), (7, 0, 4)
            for (int i = 0; i < planePolygonCount; ++i)
            {
                int outerVertex1 = firstOuterVertex + i;
                int outerVertex2 = firstOuterVertex + ((i + 1) % planePolygonCount);
                int innerVertex1 = firstInnerVertex + i;
                int innerVertex2 = firstInnerVertex + ((i + 1) % planePolygonCount);

                m_MeshIndices.Add(outerVertex1);
                m_MeshIndices.Add(outerVertex2);
                m_MeshIndices.Add(innerVertex1);

                m_MeshIndices.Add(innerVertex1);
                m_MeshIndices.Add(outerVertex2);
                m_MeshIndices.Add(innerVertex2);
            }

            m_Mesh.Clear();
            m_Mesh.SetVertices(MeshVertices);
            m_Mesh.SetTriangles(m_MeshIndices, 0);
            //m_Mesh.SetColors(m_MeshColors);
        }
        private bool _AreVerticesListsEqual(List<Vector3> firstList, List<Vector3> secondList)
        {
            if (firstList.Count != secondList.Count)
            {
                return false;
            }

            for (int i = 0; i < firstList.Count; i++)
            {
                if (firstList[i] != secondList[i])
                {
                    return false;
                }
            }

            return true;
        }
        public void SelectArea()
        {
            if (!_endDetect)
            {
                return;
            }

            if (m_PreviousFrameMeshVertices[0] != null)
            {
                minDistance = CustomMath.DistanceToPoint(PlaneCenter, m_PreviousFrameMeshVertices[0]);
            }

            foreach (Vector3 point in m_PreviousFrameMeshVertices)
            {
                float currentDistance = CustomMath.DistanceToPoint(PlaneCenter, point);
                if (minDistance > currentDistance)
                {
                    minDistance = currentDistance;
                }
            }
            float squreSide = minDistance * 2 / Mathf.Sqrt(2);

            Vector3 leftPoint = PlaneCenter - squreSide / 2 * Vector3.left;
            Vector3 rightPoint = PlaneCenter - squreSide / 2 * Vector3.right;
            Vector3 forwardPoint = PlaneCenter - squreSide / 2 * Vector3.forward;
            Vector3 backPoint = PlaneCenter - squreSide / 2 * Vector3.back;

            float x = Mathf.Round((leftPoint.x - rightPoint.x) * 10);
            float y = Mathf.Round((backPoint.z - forwardPoint.z) * 10);

            prograssText.text = (x * y * 2) + "%";

            if (x * y > 50 && cubeWorld == null)
            {
                _endDetect = false;

                prograssText.text = "100%";

                m_MeshRenderer.enabled = false;

                StartCoroutine(CreateCubeWorld());
            }

            //CreateTemp();
        }

        public void CreateTemp(Vector3 rightPoint, Vector3 leftPoint, Vector3 forwardPoint, Vector3 backPoint)
        {
            int m = 0;
            int n = 0;
            for (float i = rightPoint.x; i < leftPoint.x; i += 0.1f)
            {
                for (float j = forwardPoint.z; j < backPoint.z; j += 0.1f)
                {
                    // 높이 , 수평, 수직

                    if (m > 10 && n > 10 && _endDetect)
                    {
                        _endDetect = false;
                        prograssText.text = "생성중...";
                        m_MeshRenderer.enabled = false;
                        //Instantiate(GroundManipulatorPrefab,PlaneCenter, Quaternion.identity, transform);
                        //StartCoroutine(FixGridArray(leftPoint, rightPoint, forwardPoint, backPoint));
                        StartCoroutine(CreateCubeWorld());
                    }

                    n++;
                }
                n = 0;
                m++;
            }
        }

        double polygonArea()
        {
            int vertaxNum = m_PreviousFrameMeshVertices.Count;

            print("vertaxNum : " + vertaxNum);
            // Initialze area 
            float area = 0;
            print("vertaxNum : " + m_PreviousFrameMeshVertices[0].x);

            // Calculate value of shoelace formula 
            int j = vertaxNum - 1;
            for (int i = 0; i < vertaxNum; i++)
            {
                area += (m_PreviousFrameMeshVertices[j].x + m_PreviousFrameMeshVertices[i].x) * (m_PreviousFrameMeshVertices[j].y - m_PreviousFrameMeshVertices[i].y);
                print("area : " + (m_PreviousFrameMeshVertices[j].y));
                j = i;  // j is previous vertex to i 
            }

            // Return absolute value 
            return Mathf.Abs(area / 2f);
        }

        public void CustomAnimationControll(GameObject cubeWorld)
        {
            Animation anim = cubeWorld.AddComponent<Animation>();

            AnimationCurve curve;

            // create a new AnimationClip
            AnimationClip clip = new AnimationClip();
            clip.legacy = true;

            // create a curve to move the GameObject and assign to the clip
            Keyframe[] keys;
            keys = new Keyframe[2];
            keys[0] = new Keyframe(0.0f, 0);
            keys[1] = new Keyframe(.5f, cubeWorld.transform.localScale.x);
            curve = new AnimationCurve(keys);
            clip.SetCurve("", typeof(Transform), "localScale.x", curve);

            keys[0] = new Keyframe(0.0f, 0);
            keys[1] = new Keyframe(.5f, cubeWorld.transform.localScale.y);
            clip.SetCurve("", typeof(Transform), "localScale.y", curve);

            keys[0] = new Keyframe(0.0f, 0);
            keys[1] = new Keyframe(.5f, cubeWorld.transform.localScale.z);
            clip.SetCurve("", typeof(Transform), "localScale.z", curve);

            // update the clip to a change the red color
            //curve = AnimationCurve.Linear(0.0f, 1.0f, 2.0f, 0.0f);
            //clip.SetCurve("", typeof(Material), "_Color.r", curve);
            //clip.wrapMode = WrapMode.Loop;
            // now animate the GameObject
            anim.AddClip(clip, clip.name);
            anim.Play(clip.name);
        }

        public IEnumerator CreateCubeWorld()
        {
            cubeWorldAnchor = Session.CreateAnchor(new Pose(PlaneCenter, Quaternion.identity));

            cubeWorldAnchor.transform.SetParent(transform);

            //cubeWorldAnchor.transform.SetParent(null);

            cubeWorld = new GameObject("CubeWorld");

            //anim.clip = Resources.Load("ani1") as AnimationClip;

            cubeWorld.transform.SetParent(cubeWorldAnchor.transform);
            cubeWorld.transform.localPosition = Vector3.zero;

            CustomAnimationControll(cubeWorld);

            GroundInfo groundInfo = GroundDatabase.Instance.Get(0);
            GameObject ground = Instantiate(groundInfo.GroundPrefab);
            ground.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
            ground.transform.position = cubeWorld.transform.position;
            ground.transform.SetParent(cubeWorld.transform);
            //CreateGround(size);

            prograssText.gameObject.transform.parent.gameObject.SetActive(false);

            MainUI.Instance.OpenUI();

            yield return null;
        }
        
        public void CreateGround(int size)
        {
            float objectSize = 0.1f;
            for (int z = 0; z < size; z++)
            {
                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        //3x3 이상 큐브일 때
                        //눈에 보이지 않는 큐브는 생성하지 않음
                        if (size > 2)
                        {
                            if ((z == 0 || z == size - 1) || (y == 0 || y == size - 1) || (x == 0 || x == size - 1))
                            {
                                //get ground info
                                GroundInfo groundInfo = GroundDatabase.Instance.Get(Random.Range(1, 2));

                                GameObject ground = Instantiate(groundInfo.GroundPrefab);
                                ground.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
                                ground.transform.position = cubeWorld.transform.position
                                                            + new Vector3(x * objectSize, y * objectSize, z * objectSize)
                                                            - new Vector3(objectSize * (size / 2 - 0.5f), objectSize * (size / 2), objectSize * (size / 2 - 0.5f));
                                ground.transform.SetParent(cubeWorld.transform);


                                switch (groundInfo.groundType)
                                {
                                    case GroundType.EMPTY:
                                        //GameObject bottom_face = ground.transform.Find("bottom_face").gameObject;
                                        break;
                                    case GroundType.BUILDING_SITE:
                                        break;
                                    case GroundType.ENVIROMENT_DECO:
                                        break;
                                    case GroundType.ROAD:
                                        break;
                                    case GroundType.WARTER:
                                        break;
                                    default:
                                        break;
                                }
                            }
                        }

                    }//new Vector3(k, j, i)
                }
            }
        }

        public IEnumerator FixGridArray(Vector3 leftPoint, Vector3 rightPoint, Vector3 forwardPoint, Vector3 backPoint)
        {
            int m = 0;
            int n = 0;
            
            Anchor anchor = m_DetectedPlane.CreateAnchor(m_DetectedPlane.CenterPose);

            for (float i = rightPoint.x; i < leftPoint.x; i += 0.1f)
            {
                for (float j = forwardPoint.z; j < backPoint.z; j += 0.1f)
                {
                    GameObject manipulator = Instantiate(GroundManipulatorPrefab, (PlaneCenter.y + 0.001f) * Vector3.up + i * Vector3.right + j * Vector3.forward, Quaternion.identity, transform);

                    MapArray[m, n] = PlaneCenter.y * Vector3.up + i * Vector3.right + j * Vector3.forward;
                    n++;
                }
                n = 0;
                m++;
            }
            Gab = MapArray[0, 0];
            transform.GetComponent<DetectedPlaneVisualizer>().enabled = false;
            yield return null;
        }


        //private void OnDrawGizmos()
        //{
        //    Gizmos.color = Color.blue;
        //    Gizmos.DrawSphere(PlaneCenter, 0.1f);

        //    //for (float i = 0.1f; i < 10; i+=0.1f)
        //    //{
        //    //    Gizmos.color = Color.blue;
        //    //    Gizmos.DrawSphere(m_PlaneCenter + Vector3.right * i, 0.01f);
        //    //}
        //    if(m_PreviousFrameMeshVertices[0] != null)
        //    {
        //        minDistance = CustomMath.DistanceToPoint(PlaneCenter, m_PreviousFrameMeshVertices[0]);
        //    }

        //    foreach (Vector3 point in m_PreviousFrameMeshVertices)
        //    {
        //        Gizmos.color = Color.blue;
        //        Gizmos.DrawSphere(point, 0.1f);

        //        float currentDistance = CustomMath.DistanceToPoint(PlaneCenter, point);
        //        if(minDistance > currentDistance)
        //        {
        //            minDistance = currentDistance;
        //        }


        //    }
        //    float squreSide = minDistance * 2 / Mathf.Sqrt(2);

        //    Vector3 leftPoint = PlaneCenter - squreSide / 2 * Vector3.left;
        //    Vector3 rightPoint = PlaneCenter - squreSide / 2 * Vector3.right;
        //    Vector3 forwardPoint = PlaneCenter - squreSide / 2 * Vector3.forward;
        //    Vector3 backPoint = PlaneCenter - squreSide / 2 * Vector3.back;

        //    int m = 0;
        //    int n = 0;
        //    for (float i = rightPoint.x; i < leftPoint.x; i += 0.1f)
        //    {
        //        for (float j = forwardPoint.z; j < backPoint.z; j += 0.1f)
        //        {
        //            // 높이 , 수평, 수직
        //            //m_MapArray[m, n] = PlaneCenter.y * Vector3.up + i * Vector3.right + j * Vector3.forward;
        //            //print(m_MapArray[m, n]);
        //            //n++;+
        //            Gizmos.DrawSphere(PlaneCenter.y * Vector3.up + i * Vector3.right + j * Vector3.forward, 0.01f);

        //            if (m > 10 && n > 10 && _endDetect)
        //            {

        //                _endDetect = false;
        //                StartCoroutine(FixGridArray(leftPoint, rightPoint, forwardPoint, backPoint));
        //            }

        //            if (m > 10 && n > 10)
        //            {
        //                Gizmos.color = Color.black;
        //                Gizmos.DrawSphere(PlaneCenter, 0.01f);
        //            }
        //            n++;
        //        }
        //        n = 0;
        //        m++;
        //    }

        //    //Handles.color = Color.red;
        //    //Handles.DrawWireDisc(PlaneCenter // position
        //    //                              , new Vector3(0,1,0)                       // normal
        //    //                              , minDistance);                              // radius


        //    //Gizmos.color = Color.yellow;
        //    //Gizmos.DrawWireCube(PlaneCenter, new Vector3(squreSide, 0, squreSide));



        //    //Gizmos.color = Color.green;
        //    //Gizmos.DrawSphere(PlaneCenter - leftPoint + backPoint, 0.1f);

        //    //Gizmos.color = Color.gray;
        //    //Gizmos.DrawSphere(PlaneCenter - leftPoint + forwardPoint, 0.1f);

        //    //Gizmos.color = Color.white;
        //    //Gizmos.DrawSphere(PlaneCenter - rightPoint + forwardPoint, 0.1f);

        //    //Gizmos.color = Color.yellow;
        //    //Gizmos.DrawSphere(PlaneCenter - rightPoint + backPoint, 0.1f);

        //    //Gizmos.DrawSphere(leftPoint - Vector3.right , 0.1f);


        //}
    }
}
