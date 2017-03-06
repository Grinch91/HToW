//Class defining what a player has using BaseCharacter as parent class
public class Player : BaseCharacter {

	public int playerMorale;
	public int playerSupply;
	public string playerDeck;

	public Player()
	{
		playerMorale = StartMorale;
		playerSupply = StartSupply;
		playerDeck = CurrentDeck;
	}
}
