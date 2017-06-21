using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TimeScale : MonoBehaviour
{
    List<Vector2> vectors;
    List<Quaternion> angles;

    enum State { Null, isRecodring, isPlaying, isStopped, isPaused, isRewinding  } 

    State recorderState = State.Null;
    bool isInverse = false;

    int count = 0;
    int totalFrames = 0;

    GameObject[] recordables;
    List<Vector2>[] gvectors;
    List<Quaternion>[] gangles;

    void Start()
    {
        vectors = new List<Vector2>();
        angles = new List<Quaternion>();

        recordables = GameObject.FindGameObjectsWithTag("Recordable");

        gvectors = new List<Vector2>[recordables.Length];
        gangles = new List<Quaternion>[recordables.Length];

        for (int n = 0; n < recordables.Length; n++)
        {
            gvectors[n] = new List<Vector2>();
            gangles[n] = new List<Quaternion>();
        }
    }

    void Update()
    {
        switch (recorderState)
        {
            case State.Null:
                break;
            case State.isRecodring:
                Record(recordables);
                break;
            case State.isPlaying:
                //Play(recordables); // Корутина здесь
                break;
            case State.isStopped:
                break;
            case State.isPaused:
                Pause(recordables);
                break;
            case State.isRewinding:
                Rewind(recordables);
                break;
        }
    }

    public void Record(GameObject[] rec)
    {
        for (int b = 0; b < gvectors.Length; b++)
        {
            gvectors[b].Add(rec[b].transform.position);
            gangles[b].Add(rec[b].transform.rotation);
        }
        totalFrames = gvectors[0].Count - 1;
    }

    void Play(GameObject[] rec)
    {
        isInverse = false;
        if (count < gvectors[0].Count)
        {
            for (int b = 0; b < gvectors.Length; b++)
            {
                rec[b].transform.position = gvectors[b][count];
                rec[b].transform.rotation = gangles[b][count];
            }
            count++;
        }
        else
        {
            recorderState = State.Null; // останавливается по окончанию действия
            //count = 0; // Looping
        }
    }
    void Pause(GameObject[] rec)
    {
        if (!isInverse)
        {
            for (int b = 0; b < gvectors.Length; b++)
            {
                rec[b].transform.position = gvectors[b][count];
                rec[b].transform.rotation = gangles[b][count];
            }
        }
        if (isInverse)
        {
            for (int b = 0; b < gvectors.Length; b++)
            {
                rec[b].transform.position = gvectors[b][totalFrames];
                rec[b].transform.rotation = gangles[b][totalFrames];
            }
        }
    }

    void Rewind(GameObject[] rec)
    {
        isInverse = true;
        if (totalFrames != 0)
        {
            for (int b = 0; b < gvectors.Length; b++)
            {
                rec[b].transform.position = gvectors[b][totalFrames];
                rec[b].transform.rotation = gangles[b][totalFrames];
            }
            totalFrames--;
        }
        else
        {
            //totalFrames = gvectors[0].Count - 1; // Looping
            recorderState = State.Null; // останавливается по окончанию действия
        }
    }

    public void ChangeState(int state)
    {
        recorderState = (State)state;
    }
}