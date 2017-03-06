//Class defining what an enemy has using BaseCharacter as parent class
public class Enemy : BaseCharacter {

	public int enemyMorale;
	public int enemySupply;
	public string enemyDeck;

	public Enemy()
	{
		enemyMorale = StartMorale;
		enemySupply = StartSupply;
		enemyDeck = CurrentDeck;
	}
}
