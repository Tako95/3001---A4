/// <summary>
/// To be extended by anything that can receive damage or be destroyed by attacks
/// </summary>

public struct AttackInfo
{
    public Unit owner;
    public int attackValue;

    public AttackInfo(Unit owner, int attackValue)
    {
        this.owner = owner;
        this.attackValue = attackValue;
    }
}

public interface I_Killable
{
    public void DealDamage(AttackInfo damage);
    public void Kill();
}
