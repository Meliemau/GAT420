using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class UtilityAgent : Agent
{
    [SerializeField] Perception perception;
    [SerializeField] MeterUI meter;

    const float MIN_SCORE = 0.2f;

    Need[] needs;
    UtilityObject activeUtilityObject = null;
    public bool isUsingUtilityObject { get { return activeUtilityObject != null; } }

    public float happiness
    {
        get
        {
            float totalMotive = 0;

            foreach(var need in needs)
            {
                totalMotive += need.motive;
            }

            return 1 - (totalMotive / needs.Length);
        }
    }

    void Start()
    {
        needs = GetComponentsInChildren<Need>();
        meter.text.text = "";
    }

    void Update()
    {
        animator.SetFloat("Speed", movement.velocity.magnitude);

        if (activeUtilityObject == null)
        {
            var gameObjs = perception.GetGameObjects();

            List<UtilityObject> utilityObjects = new List<UtilityObject>();

            foreach (var go in gameObjs)
            {
                if(go.TryGetComponent<UtilityObject>(out UtilityObject utilityObject))
                {
                    utilityObject.visible = true;
                    utilityObject.score = GetUtilityObjectScore(utilityObject);
                    if(utilityObject.score > MIN_SCORE)
                    {
                        utilityObjects.Add(utilityObject);

                    }
                }
            }

            activeUtilityObject = (utilityObjects.Count == 0) ? null : utilityObjects[0];
            if(activeUtilityObject != null)
            {
                StartCoroutine(ExecuteUtilityObject(activeUtilityObject));
            }
        }
    }

    private void LateUpdate()
    {
        meter.slider.value = happiness;
        meter.worldPosition = transform.position + Vector3.up * 4;
    }

    IEnumerator ExecuteUtilityObject(UtilityObject utilityObject)
    {
        //go to location
        movement.MoveTowards(utilityObject.location.position);

        while(Vector3.Distance(transform.position, utilityObject.location.position) > 0.6f)
        {
            print(Vector3.Distance(transform.position, utilityObject.location.position));
            Debug.DrawLine(transform.position, utilityObject.location.position);

            yield return null;
        }
        print("start effect");

        //start effect
        if(utilityObject.effect != null)
        {
            utilityObject.effect.SetActive(true);
        }        

        //wait duration
        yield return new WaitForSeconds(utilityObject.duration);

        //stop effect
        if(utilityObject.effect != null)
        {
            utilityObject.effect.SetActive(false);
        }

        
        ApplyUtilityObject(utilityObject);

        activeUtilityObject = null;

        yield return null;
    }

    void ApplyUtilityObject(UtilityObject utilityObject)
    {
        foreach (var effector in utilityObject.effectors)
        {
            Need need = GetNeedByType(effector.type);

            if (need != null)
            {
                need.input += effector.change;
                need.input = Mathf.Clamp(need.input, -1, 1);
            }
        }
    }

    float GetUtilityObjectScore(UtilityObject utilityObject)
    {
        float score = 0;

        foreach(var effector in utilityObject.effectors)
        {
            Need need = GetNeedByType(effector.type);
            if (need != null)
            {
                float futureNeed = need.getMotive(need.input + effector.change);
                score += need.motive - futureNeed;
            }
        }

        return score;
    }

    UtilityObject GetHighestUtilityObject(UtilityObject[] utilityObjects)
    {
        UtilityObject highestUtilityObject = null;
        float highestScore = MIN_SCORE;

        foreach(var utilityObject in utilityObjects)
        {
            var utilityScore = GetUtilityObjectScore(utilityObject);
            if(utilityScore > highestScore)
            {
                highestUtilityObject = utilityObject;
            }
        }

        return highestUtilityObject;
    }

    UtilityObject GetRandomUtilityObject(UtilityObject[] utilityObjects)
    {
        float[] scores = new float[utilityObjects.Length];
        float totalScore = 0;

        for (int i = 0; i < utilityObjects.Length; i++)
        {
            var utilityScore = GetUtilityObjectScore(utilityObjects[i]);
            scores[i] = utilityScore;
            totalScore += scores[i];
        }

        //select random utility object based on score
        //higher the score higher chance of being randomly selected
        float random = Random.Range(0, totalScore);
        for(int i = 0; i < scores.Length; i++)
        {
            if(random < scores[i])
            {
                return utilityObjects[i];
            }

            random -= scores[i];
        }

        return null;
    }

    Need GetNeedByType(Need.Type type)
    {
        return needs.First(need => need.type == type);
    }

    private void OnGUI()
    {
        Vector2 screen = Camera.main.WorldToScreenPoint(transform.position);

        GUI.color = Color.black;
        int offset = 0;
        foreach (var need in needs)
        {
            GUI.Label(new Rect(screen.x + 20, Screen.height - screen.y - offset, 300, 20), need.type.ToString() + ": " + need.motive);
            offset += 20;
        }
        //GUI.Label(new Rect(screen.x + 20, Screen.height - screen.y - offset, 300, 20), mood.ToString());
    }
}
