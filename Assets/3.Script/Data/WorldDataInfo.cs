
[System.Serializable]
public class WorldDataInfo
{
    public int worldIndex;      // 월드 인덱스
    public string worldNameKey;    // 월드 이름
    public int startRow;        // 해당 월드에 속한 스테이지의 첫 스테이지열
    public int endRow;          // 해당 월드에 속한 스테이지의 마지막 스테이지열
    public string background;   // 해당 월드에 해당하는 이미지 파일 이름

    public void SetWorldDataInfo(int worldIndex, string worldNameKey, int startRow, int endRow, string background)
    {
        this.worldIndex = worldIndex;
        this.worldNameKey = worldNameKey;
        this.startRow = startRow;
        this.endRow = endRow;
        this.background = background;
    }
}
