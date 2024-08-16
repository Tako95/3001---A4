public interface IAttackCommandable
{
    /// <summary>
    /// Should return true if command is successful, false otherwise
    /// </summary>
    /// <param name="target"></param>
    /// <returns></returns>
    public bool Attack(Unit target);
}
