using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using Unity.VisualScripting;
using UnityEngine;
using Valve.VR.InteractionSystem;

[System.Serializable]
public struct CircularDriveObjects
{
    public GameObject up;
    public GameObject down;
    public GameObject front;
    public GameObject back;
    public GameObject right;
    public GameObject left;
};

[System.Serializable]
public struct CircularDrives
{
    public CircularDrive up;
    public CircularDrive down;
    public CircularDrive front;
    public CircularDrive back;
    public CircularDrive right;
    public CircularDrive left;
};

[System.Serializable]
public struct Cubes
{
    public Transform U;
    public Transform D;
    public Transform F;
    public Transform B;
    public Transform R;
    public Transform L;

    public Transform UF;
    public Transform UB;
    public Transform UR;
    public Transform UL;
    public Transform UFR;
    public Transform UFL;
    public Transform UBR;
    public Transform UBL;

    public Transform DF;
    public Transform DB;
    public Transform DR;
    public Transform DL;
    public Transform DFR;
    public Transform DFL;
    public Transform DBR;
    public Transform DBL;

    public Transform FR;
    public Transform FL;
    public Transform BR;
    public Transform BL;

    public Transform middle;
};



public class Rubik : MonoBehaviour
{
    public CircularDriveObjects circularDriveObjects;
    public CircularDrives circularDrives;
    public Cubes cubes;

    public Transform rubikParent;
    public Transform rotationParent;

    public float snapSpeed = 90.0f;
    public float cubesOffset = 0.11f;
    public uint shuffleMovesCountMin = 20;
    public uint shuffleMovesCountMax = 30;
    
    // [x,y,z] -> [down-up, left-right, front-back]
    Transform[,,] cubeArray = new Transform [3,3,3];
    GameObject[] circularDriveObjectsArray = new GameObject [6];
    CircularDrive[] circularDrivesArray = new CircularDrive [6];


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

    enum Moves
    {
        U_CW = 0,
        U_CCW,
        D_CW,
        DD_CCW,
        F_CW,
        F_CCW,
        B_CW,
        B_CCW,
        R_CW,
        R_CCW,
        L_CW,
        L_CCW
    }

    UsedSide usedSide = UsedSide.None;
    State state = State.IDLE;

    float prevAngle = 0.0f;

    float angle = 0.0f;
    float snapDirection = 0.0f;
    float snapAngle = 0.0f;
    float snapRotationOffset = 0.0f;
    float diffAngle = 0.0f;

    float angleAccumulator;

    Vector3 tempVec3 = new Vector3();

    void Start()
    {
        if(shuffleMovesCountMin <= 0)
        {
            Debug.LogError("shuffleMovesCountMin value must be greater than 0");
        }

        if(shuffleMovesCountMin > shuffleMovesCountMax)
        {
            Debug.LogError("value shuffleMovesCountMin must be not greater than shuffleMovesCountMax");
        }

        if((shuffleMovesCountMin > 2000) || (shuffleMovesCountMax > 2000))
        {
            Debug.LogError("Values shuffleMovesCountMin and shuffleMovesCountMax must be no greater than 2000");
        }


        cubeArray[0,0,0] = cubes.DFL;
        cubeArray[0,0,1] = cubes.DL;
        cubeArray[0,0,2] = cubes.DBL;

        cubeArray[0,1,0] = cubes.DF;
        cubeArray[0,1,1] = cubes.D;
        cubeArray[0,1,2] = cubes.DB;

        cubeArray[0,2,0] = cubes.DFR;
        cubeArray[0,2,1] = cubes.DR;
        cubeArray[0,2,2] = cubes.DBR;

        
        cubeArray[1,0,0] = cubes.FL;
        cubeArray[1,0,1] = cubes.L;
        cubeArray[1,0,2] = cubes.BL;

        cubeArray[1,1,0] = cubes.F;
        cubeArray[1,1,1] = cubes.middle;
        cubeArray[1,1,2] = cubes.B;

        cubeArray[1,2,0] = cubes.FR;
        cubeArray[1,2,1] = cubes.R;
        cubeArray[1,2,2] = cubes.BR;


        cubeArray[2,0,0] = cubes.UFL;
        cubeArray[2,0,1] = cubes.UL;
        cubeArray[2,0,2] = cubes.UBL;

        cubeArray[2,1,0] = cubes.UF;
        cubeArray[2,1,1] = cubes.U;
        cubeArray[2,1,2] = cubes.UB;

        cubeArray[2,2,0] = cubes.UFR;
        cubeArray[2,2,1] = cubes.UR;
        cubeArray[2,2,2] = cubes.UBR;


        circularDriveObjectsArray[(int)UsedSide.Up] = circularDriveObjects.up;
        circularDriveObjectsArray[(int)UsedSide.Down] = circularDriveObjects.down;
        circularDriveObjectsArray[(int)UsedSide.Front] = circularDriveObjects.front;
        circularDriveObjectsArray[(int)UsedSide.Back] = circularDriveObjects.back;
        circularDriveObjectsArray[(int)UsedSide.Right] = circularDriveObjects.right;
        circularDriveObjectsArray[(int)UsedSide.Left] = circularDriveObjects.left;

        circularDrivesArray[(int)UsedSide.Up] = circularDrives.up;
        circularDrivesArray[(int)UsedSide.Down] = circularDrives.down;
        circularDrivesArray[(int)UsedSide.Front] = circularDrives.front;
        circularDrivesArray[(int)UsedSide.Back] = circularDrives.back;
        circularDrivesArray[(int)UsedSide.Right] = circularDrives.right;
        circularDrivesArray[(int)UsedSide.Left] = circularDrives.left;

    }

    void Update()
    {
        switch(state)
        {
            case State.IDLE:
            {
                for(int drive = 0; drive < 6; drive++)
                {
                    if(circularDrivesArray[drive].IsGrabbed())
                    {
                        usedSide = (UsedSide)drive;
                        prevAngle = circularDrivesArray[drive].GetOutAngle();

                        for(int i = 0; i < 6; i++)
                        {
                            if(i != (int)usedSide)
                            {
                                circularDriveObjectsArray[i].SetActive(false);
                            }
                        }
                        
                        AssignCubesToRotate((UsedSide)drive);

                        state = State.ROTATING;
                        angleAccumulator = 0.0f;
                        break;
                    }
                }
            }
            break;

            case State.ROTATING:
            {
                if(circularDrivesArray[(int)usedSide].IsGrabbed() == false)
                {
                    state = State.CALCULATE_SNAP;
                    circularDriveObjectsArray[(int)usedSide].SetActive(false);
                    break;
                }

                float currentAngle = circularDrivesArray[(int)usedSide].GetOutAngle();
                diffAngle = currentAngle - prevAngle;

                while(circularDrivesArray[(int)usedSide].GetOutAngle() > 180.0f)
                {
                    circularDrivesArray[(int)usedSide].SetOutAngle(circularDrivesArray[(int)usedSide].GetOutAngle() - 360.0f);
                }

                while(circularDrivesArray[(int)usedSide].GetOutAngle() <= -180.0f)
                {
                    circularDrivesArray[(int)usedSide].SetOutAngle(circularDrivesArray[(int)usedSide].GetOutAngle() + 360.0f);
                }

                prevAngle = circularDrivesArray[(int)usedSide].GetOutAngle();

                angleAccumulator += diffAngle;

                while(angleAccumulator >= 360.0f)
                {
                    angleAccumulator -= 360.0f;
                }

                while(angleAccumulator < 0.0f)
                {
                    angleAccumulator += 360.0f;
                }

                GetRotationAxis(usedSide, ref rotationAxis);

                rotationParent.localEulerAngles = rotationAxis * angleAccumulator;
            }
            break;

            case State.CALCULATE_SNAP:
            {

                angle = GetRotationParentRotationAngle(usedSide);

                if(angle < 0.0f)
                {
                    angle += 360.0f;
                }
                else if(angle >= 360.0f)
                {
                    angle -= 360.0f;
                }

                snapRotationOffset = angle % 90.0f;

                if(snapRotationOffset < 45.0f)
                {
                    snapDirection = -1.0f;
                }
                else
                {
                    snapDirection = 1.0f;
                    snapRotationOffset = 90.0f - snapRotationOffset;
                }

                snapAngle = MathF.Round(angle + (snapDirection * snapRotationOffset));
                //snapAngle %= 360.0f;

                state = State.SNAP;
            }
            break;

            case State.SNAP:
            {
                if(Time.deltaTime * snapSpeed > snapRotationOffset)
                {
                    diffAngle = snapRotationOffset;
                    snapRotationOffset = 0.0f;
                }
                else
                {
                    diffAngle = Time.deltaTime * snapSpeed * snapDirection;
                    snapRotationOffset -= Time.deltaTime * snapSpeed;
                }

                GetRotationParentRotationAxis(usedSide, ref rotationAxis);

                rotationParent.RotateAround(rotationParent.position, rotationAxis, diffAngle);

                if(snapRotationOffset <= 0.0f)
                {
                    GetRotationAxis(usedSide, ref rotationAxis);

                    tempVec3 = rotationAxis * snapAngle;
                
                    rotationParent.localEulerAngles = tempVec3;

                    for(int i = 0; i < 3; i++)
                    {
                        for(int j = 0; j < 3; j++)
                        {
                            rotatingCubes[i,j].SetParent(rubikParent, true);
                        }
                        
                    }

                    rotationParent.localEulerAngles = Vector3.zero;

                    for(int i = 0; i < 6; i++)
                    {
                        circularDriveObjectsArray[i].SetActive(true);
                    }
                
                    RotateCubes(usedSide);

                    usedSide = UsedSide.None;
                    state = State.IDLE;
                    break;
                }
            }
            break;
        }
    }


    void AssignCubesToRotate(UsedSide side)
    {  
        int du = 0;
        int lr = 0;

        switch(side)
        {
            case UsedSide.Up:
            {
                for(int yID = 2; yID >= 0; yID--)
                {
                    du = 0;

                    for(int zID = 2; zID >= 0; zID--)
                    {
                        cubeArray[2, yID, zID].SetParent(rotationParent, true);
                        rotatingCubes[du, lr] = cubeArray[2, yID, zID];

                        du++;
                    }
                    
                    lr++;
                }
            }
            break;

            case UsedSide.Down:
            {
                for(int yID = 0; yID < 3; yID++)
                {
                    du = 0;

                    for(int zID = 2; zID >= 0 ; zID--)
                    {
                        cubeArray[0, yID, zID].SetParent(rotationParent, true);
                        rotatingCubes[du, lr] = cubeArray[0, yID, zID];

                        du++;
                    }

                    lr++;
                }
            }
            break;

            case UsedSide.Front:
            {
                for(int xID = 0; xID < 3; xID++)
                {
                    lr = 0;

                    for(int yID = 0; yID < 3; yID++)
                    {
                        cubeArray[xID, yID, 0].SetParent(rotationParent, true);
                        rotatingCubes[du, lr] = cubeArray[xID, yID, 0];

                        lr++;
                    }

                    du++;
                }
            }
            break;

            case UsedSide.Back:
            {
                for(int xID = 0; xID < 3; xID++)
                {
                    lr = 0;

                    for(int yID = 2; yID >= 0; yID--)
                    {
                        cubeArray[xID, yID, 2].SetParent(rotationParent, true);
                        rotatingCubes[du, lr] = cubeArray[xID, yID, 2];

                        lr++;
                    }

                    du++;
                }
            }
            break;

            case UsedSide.Right:
            {
                for(int xID = 0; xID < 3; xID++)
                {
                    lr = 0;

                    for(int zID = 0; zID < 3; zID++)
                    {
                        cubeArray[xID, 2, zID].SetParent(rotationParent, true);
                        rotatingCubes[du, lr] = cubeArray[xID, 2, zID];

                        lr++;
                    }

                    du++;
                }
            }
            break;

            case UsedSide.Left:
            {
                for(int xID = 0; xID < 3; xID++)
                {
                    lr = 0;

                    for(int zID = 2; zID >= 0; zID--)
                    {
                        cubeArray[xID, 0, zID].SetParent(rotationParent, true);
                        rotatingCubes[du, lr] = cubeArray[xID, 0, zID];

                        lr++;
                    }

                    du++;
                }
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

    void GetRotationParentRotationAxis(UsedSide side, ref Vector3 rotationAxis)
    {
        switch(side)
        {
            case UsedSide.Up:
            {
                rotationAxis = rotationParent.up;
            }
            break;

            case UsedSide.Down:
            {
                rotationAxis = -rotationParent.up;
            }
            break;

            case UsedSide.Front:
            {
                rotationAxis = rotationParent.forward;
            }
            break;

            case UsedSide.Back:
            {
                rotationAxis = -rotationParent.forward;
            }
            break;

            case UsedSide.Right:
            {
                rotationAxis = rotationParent.right;
            }
            break;

            case UsedSide.Left:
            {
                rotationAxis = -rotationParent.right;
            }
            break;
        }
    }

    float GetRotationParentRotationAngle(UsedSide side)
    {
        switch(side)
        {
            case UsedSide.Up:
            {
                if(((rotationParent.localEulerAngles.x  % 360.0f) == 180.0f) || ((rotationParent.localEulerAngles.z % 360.0f) == 180.0f))
                {
                    return ((180.0f - rotationParent.localEulerAngles.y) % 360.0f);
                }

                return rotationParent.localEulerAngles.y;
            }

            case UsedSide.Down:
            {
                if(((rotationParent.localEulerAngles.x  % 360.0f) == 180.0f) || ((rotationParent.localEulerAngles.z % 360.0f) == 180.0f))
                {
                    return -((180.0f - rotationParent.localEulerAngles.y) % 360.0f);
                }

                return -rotationParent.localEulerAngles.y;
            }

            case UsedSide.Front:
            {
                if(((rotationParent.localEulerAngles.x  % 360.0f) == 180.0f) || ((rotationParent.localEulerAngles.y % 360.0f) == 180.0f))
                {
                    return ((180.0f - rotationParent.localEulerAngles.z) % 360.0f);
                }

                return rotationParent.localEulerAngles.z;
            }

            case UsedSide.Back:
            {
                if(((rotationParent.localEulerAngles.x  % 360.0f) == 180.0f) || ((rotationParent.localEulerAngles.y % 360.0f) == 180.0f))
                {
                    return -((180.0f - rotationParent.localEulerAngles.z) % 360.0f);
                }

                return -rotationParent.localEulerAngles.z;
            }

            case UsedSide.Right:
            {
                if(((rotationParent.localEulerAngles.y  % 360.0f) == 180.0f) || ((rotationParent.localEulerAngles.z % 360.0f) == 180.0f))
                {
                    return ((180.0f - rotationParent.localEulerAngles.x) % 360.0f);
                }

                return rotationParent.localEulerAngles.x;
            }

            case UsedSide.Left:
            {
                if(((rotationParent.localEulerAngles.y  % 360.0f) == 180.0f) || ((rotationParent.localEulerAngles.z % 360.0f) == 180.0f))
                {
                    return -((180.0f - rotationParent.localEulerAngles.x) % 360.0f);
                }

                return -rotationParent.localEulerAngles.x;
            }

            default:
            {
                return 0.0f;
            }
        }
    }

    void RotateCubes(UsedSide side)
    {
        int rotateCount = 0;

        if((snapAngle == 0.0f) || (snapAngle == 360.0f))
        {
            return;
        }
        else if(snapAngle == 90.0f)
        {
            if((usedSide == UsedSide.Right) || (usedSide == UsedSide.Left))
            {
                rotateCount = -1;
            }
            else
            {
                rotateCount = 1;
            }
        }
        else if(snapAngle == 180.0f)
        {
            rotateCount = 2;
        }
        else if(snapAngle == 270.0f)
        {
            if((usedSide == UsedSide.Right) || (usedSide == UsedSide.Left))
            {
                rotateCount = 1;
            }
            else
            {
                rotateCount = -1;
            }
        }

        Transform buffer;

        if(rotateCount > 0)
        {
            // rotate right

            for(int i = 0; i < rotateCount; i++)
            {
                buffer = rotatingCubes[2,1];
                rotatingCubes[2,1] = rotatingCubes[1,0];
                rotatingCubes[1,0] = rotatingCubes[0,1];
                rotatingCubes[0,1] = rotatingCubes[1,2];
                rotatingCubes[1,2] = buffer;

                buffer = rotatingCubes[2,2];
                rotatingCubes[2,2] = rotatingCubes[2,0];
                rotatingCubes[2,0] = rotatingCubes[0,0];
                rotatingCubes[0,0] = rotatingCubes[0,2];
                rotatingCubes[0,2] = buffer;
            }
        }
        else
        {
            //rotate left

            buffer = rotatingCubes[2,1];
            rotatingCubes[2,1] = rotatingCubes[1,2];
            rotatingCubes[1,2] = rotatingCubes[0,1];
            rotatingCubes[0,1] = rotatingCubes[1,0];
            rotatingCubes[1,0] = buffer;

            buffer = rotatingCubes[2,2];
            rotatingCubes[2,2] = rotatingCubes[0,2];
            rotatingCubes[0,2] = rotatingCubes[0,0];
            rotatingCubes[0,0] = rotatingCubes[2,0];
            rotatingCubes[2,0] = buffer;
        }

        int du = 0;
        int lr = 0;

        switch(side)
        {
            case UsedSide.Up:
            {
                for(int yID = 2; yID >= 0; yID--)
                {
                    du = 0;

                    for(int zID = 2; zID >= 0; zID--)
                    {
                        cubeArray[2, yID, zID] = rotatingCubes[du, lr];

                        du++;
                    }
                    
                    lr++;
                }
            }
            break;

            case UsedSide.Down:
            {
                for(int yID = 0; yID < 3; yID++)
                {
                    du = 0;

                    for(int zID = 2; zID >= 0 ; zID--)
                    {
                        cubeArray[0, yID, zID] = rotatingCubes[du, lr];

                        du++;
                    }

                    lr++;
                }
            }
            break;

            case UsedSide.Front:
            {
                for(int xID = 0; xID < 3; xID++)
                {
                    lr = 0;

                    for(int yID = 0; yID < 3; yID++)
                    {
                        cubeArray[xID, yID, 0] = rotatingCubes[du, lr];

                        lr++;
                    }

                    du++;
                }
            }
            break;

            case UsedSide.Back:
            {
                for(int xID = 0; xID < 3; xID++)
                {
                    lr = 0;

                    for(int yID = 2; yID >= 0; yID--)
                    {
                        cubeArray[xID, yID, 2] = rotatingCubes[du, lr];

                        lr++;
                    }

                    du++;
                }
            }
            break;

            case UsedSide.Right:
            {
                for(int xID = 0; xID < 3; xID++)
                {
                    lr = 0;

                    for(int zID = 0; zID < 3; zID++)
                    {
                        cubeArray[xID, 2, zID] = rotatingCubes[du, lr];

                        lr++;
                    }

                    du++;
                }
            }
            break;

            case UsedSide.Left:
            {
                for(int xID = 0; xID < 3; xID++)
                {
                    lr = 0;

                    for(int zID = 2; zID >= 0; zID--)
                    {
                        cubeArray[xID, 0, zID] = rotatingCubes[du, lr];

                        lr++;
                    }

                    du++;
                }
            }
            break;
        }
    }


    public void ResetCubeRotation(Hand hand)
    {
        transform.localRotation = Quaternion.Euler(0.0f, 0.0f, 0.0f);
    }

    public void ResetCubeState(Hand hand)
    {
        cubeArray[0,0,0] = cubes.DFL;
        cubeArray[0,0,1] = cubes.DL;
        cubeArray[0,0,2] = cubes.DBL;

        cubeArray[0,1,0] = cubes.DF;
        cubeArray[0,1,1] = cubes.D;
        cubeArray[0,1,2] = cubes.DB;

        cubeArray[0,2,0] = cubes.DFR;
        cubeArray[0,2,1] = cubes.DR;
        cubeArray[0,2,2] = cubes.DBR;

        
        cubeArray[1,0,0] = cubes.FL;
        cubeArray[1,0,1] = cubes.L;
        cubeArray[1,0,2] = cubes.BL;

        cubeArray[1,1,0] = cubes.F;
        cubeArray[1,1,1] = cubes.middle;
        cubeArray[1,1,2] = cubes.B;

        cubeArray[1,2,0] = cubes.FR;
        cubeArray[1,2,1] = cubes.R;
        cubeArray[1,2,2] = cubes.BR;


        cubeArray[2,0,0] = cubes.UFL;
        cubeArray[2,0,1] = cubes.UL;
        cubeArray[2,0,2] = cubes.UBL;

        cubeArray[2,1,0] = cubes.UF;
        cubeArray[2,1,1] = cubes.U;
        cubeArray[2,1,2] = cubes.UB;

        cubeArray[2,2,0] = cubes.UFR;
        cubeArray[2,2,1] = cubes.UR;
        cubeArray[2,2,2] = cubes.UBR;

        // [x,y,z] -> [down-up, left-right, front-back]
        for(int x = 0; x < 3; x++)
        {
            for(int y = 0; y < 3; y++)
            {
                for(int z = 0; z < 3; z++)
                {
                    tempVec3.x = -(y - 1) * cubesOffset;
                    tempVec3.y = (x - 1) * cubesOffset;
                    tempVec3.z = -(z - 1) * cubesOffset;

                    cubeArray[x,y,z].SetLocalPositionAndRotation(tempVec3, Quaternion.Euler(0.0f, 0.0f, 0.0f));
                }
            }
        }
    }


    public void ShuffleCube(Hand hand)
    {
        int shuffleMovesNum = UnityEngine.Random.Range((int)shuffleMovesCountMin, (int)shuffleMovesCountMax + 1);

        for(int i = 0; i < shuffleMovesNum; i++)
        {   
            usedSide = (UsedSide)UnityEngine.Random.Range(1, 7);

            if(UnityEngine.Random.Range(0, 2) == 0)
            {
                // function RotateCubes uses this variable to perform rotation of cubes in array
                snapAngle = 90.0f;
            }
            else
            {
                // function RotateCubes uses this variable to perform rotation of cubes in array
                snapAngle = 270.0f;
            }

            AssignCubesToRotate(usedSide);

            GetRotationAxis(usedSide, ref rotationAxis);

            tempVec3 = rotationAxis * snapAngle;
        
            rotationParent.localEulerAngles = tempVec3;

            for(int j = 0; j < 3; j++)
            {
                for(int k = 0; k < 3; k++)
                {
                    rotatingCubes[j,k].SetParent(rubikParent, true);
                }
            }

            rotationParent.localEulerAngles = Vector3.zero;

            RotateCubes(usedSide);
        }

        usedSide = UsedSide.None;
    }

}






