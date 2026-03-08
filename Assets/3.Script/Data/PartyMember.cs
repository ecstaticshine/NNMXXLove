using System;

[Serializable]
public class PartyMember
{
    public int slotIndex; // 0 ~ 8
    public int unitID;    // 10101

    public PartyMember(int slotIndex, int unitID)
    {
        this.slotIndex = slotIndex;
        this.unitID = unitID;
    }
}
