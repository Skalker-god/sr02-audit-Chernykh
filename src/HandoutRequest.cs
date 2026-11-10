namespace PickupPoint;

// R11: параметри сценарію видачі зібрані в один об'єкт.
// Прапорці лишаються внутрішніми: публічні методи після R19 не приймають bool.
public class HandoutRequest
{
    public string Code;
    public string Phone;
    public bool IsReturn;
    public bool SkipCheck;
    public bool Partial;
}
