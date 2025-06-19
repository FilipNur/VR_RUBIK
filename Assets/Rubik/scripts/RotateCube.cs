using System;
using System.Runtime.InteropServices.WindowsRuntime;
using Unity.VisualScripting;
using UnityEngine;
using Valve.VR.InteractionSystem;

[System.Serializable]
public struct CircularDriveObject_rotation
{
    public GameObject front;
    public GameObject back;
    public GameObject right;
    public GameObject left;
};

[System.Serializable]
public struct CircularDrives_rotation
{
    public CircularDrive front;
    public CircularDrive back;
    public CircularDrive right;
    public CircularDrive left;
};



public class RotateCube : MonoBehaviour
{
    public CircularDriveObject_rotation circularDriveObjects;
    public CircularDrives_rotation circularDrives;

    public Transform rubik;

    public float snapSpeed = 90.0f;
    
    GameObject[] CircularDriveObject_rotationArray = new GameObject [6];
    CircularDrive[] CircularDrives_rotationArray = new CircularDrive [6];

    GameObject rotationPArent;


    Vector3 rotationAxis = new Vector3();

    // [x,y] -> [down-up, left-right]
    // up or front side is at the top (up has bigger priority)
    Transform[,] rotatingCubes = new Transform [3,3];


    enum UsedSide
    {
        Up = 0,
        Down,
        Front,
        Back,
        Right,
        Left,
        None
    }

    enum State
    {
        IDLE = 0,
        ROTATING,
        CALCULATE_SNAP,
        SNAP
    }

    UsedSide usedSide = UsedSide.None;
    State state = State.IDLE;

    float prevAngle = 0.0f;

    float diffAngle = 0.0f;

    void Start()
    {
        CircularDriveObject_rotationArray[(int)UsedSide.Front] = circularDriveObjects.front;
        CircularDriveObject_rotationArray[(int)UsedSide.Back] = circularDriveObjects.back;
        CircularDriveObject_rotationArray[(int)UsedSide.Right] = circularDriveObjects.right;
        CircularDriveObject_rotationArray[(int)UsedSide.Left] = circularDriveObjects.left;

        CircularDrives_rotationArray[(int)UsedSide.Front] = circularDrives.front;
        CircularDrives_rotationArray[(int)UsedSide.Back] = circularDrives.back;
        CircularDrives_rotationArray[(int)UsedSide.Right] = circularDrives.right;
        CircularDrives_rotationArray[(int)UsedSide.Left] = circularDrives.left;

    }

    void Update()
    {
        switch(state)
        {
            case State.IDLE:
            {
                for(int drive = 2; drive < 6; drive++)
                {
                    if(CircularDrives_rotationArray[drive].IsGrabbed())
                    {
                        usedSide = (UsedSide)drive;
                        prevAngle = CircularDrives_rotationArray[drive].GetOutAngle();

                        for(int i = 2; i < 6; i++)
                        {
                            if(i != (int)usedSide)
                            {
                                CircularDriveObject_rotationArray[i].SetActive(false);
                            }
                        }

                        state = State.ROTATING;
                        break;
                    }
                }
            }
            break;

            case State.ROTATING:
            {
                if(CircularDrives_rotationArray[(int)usedSide].IsGrabbed() == false)
                {
                    state = State.IDLE;

                    for(int i = 2; i < 6; i++)
                    {
                        if(i != (int)usedSide)
                        {
                            CircularDriveObject_rotationArray[i].SetActive(true);
                        }
                    }
                    break;
                }

                float currentAngle = CircularDrives_rotationArray[(int)usedSide].GetOutAngle();
                diffAngle = currentAngle - prevAngle;

                while(CircularDrives_rotationArray[(int)usedSide].GetOutAngle() > 180.0f)
                {
                    CircularDrives_rotationArray[(int)usedSide].SetOutAngle(CircularDrives_rotationArray[(int)usedSide].GetOutAngle() - 360.0f);
                }

                while(CircularDrives_rotationArray[(int)usedSide].GetOutAngle() <= -180.0f)
                {
                    CircularDrives_rotationArray[(int)usedSide].SetOutAngle(CircularDrives_rotationArray[(int)usedSide].GetOutAngle() + 360.0f);
                }

                prevAngle = CircularDrives_rotationArray[(int)usedSide].GetOutAngle();

                GetRotationAxis(usedSide, ref rotationAxis);

                rubik.RotateAround(rubik.position ,rotationAxis, diffAngle);
            }
            break;
        }
    }

    void GetRotationAxis(UsedSide side, ref Vector3 rotationAxis)
    {
        switch(side)
        {
            case UsedSide.Up:
            {
                rotationAxis = Vector3.up;
            }
            break;

            case UsedSide.Down:
            {
                rotationAxis = Vector3.down;
            }
            break;

            case UsedSide.Front:
            {
                rotationAxis = Vector3.forward;
            }
            break;

            case UsedSide.Back:
            {
                rotationAxis = Vector3.back;
            }
            break;

            case UsedSide.Right:
            {
                rotationAxis = Vector3.right;
            }
            break;

            case UsedSide.Left:
            {
                rotationAxis = Vector3.left;
            }
            break;
        }
    }
}




