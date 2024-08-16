using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(I_IFFChallengeable))]
public class UnitTeamColorer : MonoBehaviour
{
    [SerializeField]
    Unit me;

    [SerializeField]
    List<MeshRenderer> renderers;

    static int unitID = 0;

    // Start is called before the first frame update
    void Start()
    {
        if(!me)
        {
            me = GetComponent<Unit>();
        }
        me.name = me.GetUnitName() + " " + unitID.ToString();
        unitID++;
        UpdateColor();
        me.eventOnHit.AddListener(OnHitListener);
    }

    private void OnHitListener(Unit unitHit, AttackInfo attack)
    {
        UpdateColor();
    }

    public void UpdateColor()
    {
        float brightness = Mathf.Clamp01(0.2f + (0.8f * ((float)me.HitPoints / (float)me.HitPointsMax)));

        UpdateColor(renderers, me.Team, brightness);
    }

    public static void UpdateColor(List<MeshRenderer> renderers, Team team, float brightness)
    {
        Color color = Color.black;
        switch (team)
        {
            case Team.Blue:
                {
                    color = Color.blue;
                    break;
                }
            case Team.Red:
                {
                    color = Color.red;
                    break;
                }
            default:
                {
                    break;
                }
        }
        color *= brightness;
        color.a = 1;

        foreach (MeshRenderer renderer in renderers)
        {
            foreach (Material mat in renderer.materials)
            {
                if(mat.name.StartsWith("MainUnitColor"))
                {
                    mat.SetColor("_Color", color);
                }
            }
        }
    }
}
