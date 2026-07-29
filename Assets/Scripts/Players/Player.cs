//Class defining what a player has using BaseCharacter as parent class
public class Player : BaseCharacter {

	public int playerMorale;

	/// <summary>Supply available to spend this turn.</summary>
	public int playerSupply;

	/// <summary>
	/// The ceiling supply refills to at the start of each of this side's turns. Grows by
	/// one per turn up to MaxSupply. Starts one below StartSupply so the first turn's
	/// increment lands exactly on StartSupply.
	/// </summary>
	public int playerSupplyCap;

	public string playerDeck;

	public Player()
	{
		playerMorale = StartMorale;
		playerSupply = StartSupply;
		playerSupplyCap = StartSupply - 1;
		playerDeck = CurrentDeck;
	}
}
