using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum SkillArea { Single, Row, Column, All, CrossRange, Cross} // 단일, 가로줄, 세로줄, 전체

[System.Serializable]
public class SkillInfo
{
    public string skillName;
    public float multiplier;
    public SkillArea areaType;
    public int skillLevel;
}
