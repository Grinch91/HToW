//Class defining what an enemy has using BaseCharacter as parent class
public class Enemy : BaseCharacter {

	public int enemyMorale;

	/// <summary>Supply available to spend this turn.</summary>
	public int enemySupply;

	/// <summary>Ceiling supply refills to each turn. See <see cref="Player.playerSupplyCap"/>.</summary>
	public int enemySupplyCap;

	public string enemyDeck;

	public Enemy()
	{
		enemyMorale = StartMorale;
		enemySupply = StartSupply;
		enemySupplyCap = StartSupply - 1;
		enemyDeck = CurrentDeck;
	}
}
