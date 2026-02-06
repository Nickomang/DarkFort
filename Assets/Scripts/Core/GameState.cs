namespace DarkFort.Core
{
    /// <summary>
    /// Defines all possible game states
    /// </summary>
    public enum GameState
    {
        /// <summary>
        /// Player is in the main menu
        /// </summary>
        MainMenu,

        /// <summary>
        /// Player is exploring the dungeon, moving between rooms
        /// </summary>
        Exploring,

        /// <summary>
        /// Player is engaged in combat with a monster
        /// </summary>
        Combat,

        /// <summary>
        /// Player is interacting with a merchant
        /// </summary>
        Merchant,

        /// <summary>
        /// Game is paused
        /// </summary>
        Paused,

        /// <summary>
        /// Game has ended (victory or defeat)
        /// </summary>
        GameOver
    }
}