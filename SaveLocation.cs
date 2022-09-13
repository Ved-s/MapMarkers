namespace MapMarkers
{
    /// <summary>
    /// Marker save location
    /// </summary>
    public enum SaveLocation 
    {
        /// <summary>
        /// Don't save the marker
        /// </summary>
        None,

        /// <summary>
        /// Save on multiplayer client or singleplayer world
        /// </summary>
        Client,

        /// <summary>
        /// Save on server or singleplayer world
        /// </summary>
        Server
    }
}
