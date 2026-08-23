namespace FigmaRuntime
{
    /// <summary>
    /// v0.5 (ADR-025): a FigmaFlow input-réteg interfésze.
    ///
    /// A runtime NEM kötött egyetlen input-forráshoz — a demók, a
    /// PlayMaker és a jövőbeli külső input-réteg (érintőfal, lidar)
    /// ezen az interfészen csatlakozik:
    /// - a demó input-kezelők (Dialog/BottomSheet/Popup/Hero) az
    ///   absztrakt FigmaFlowInputBase-ből származnak
    /// - a PlayMaker a kattintás-észleléshez nem a ClickEvent-re
    ///   hagyatkozik, hanem az Activate() jelekre (a jövőbeli lidar
    ///   is így jelez)
    ///
    /// Az interfész két irányt definiál:
    /// - az input → runtime: GoToState (a felhasználói navigáció)
    /// - az input → PlayMaker: Activate (aktivitás-jelzés az idle-hez)
    /// </summary>
    public interface IFigmaFlowInput
    {
        /// <summary>
        /// A kezelt FigmaStateMachine.
        /// </summary>
        FigmaStateMachine Machine { get; }

        /// <summary>
        /// Az input bekötése (a UI eseményekre való feliratkozás).
        /// </summary>
        void Bind();

        /// <summary>
        /// Az input lecsatlakoztatása.
        /// </summary>
        void Unbind();

        /// <summary>
        /// Aktivitás-jelzés — az idle-időzítő (PlayMaker) resetje.
        /// A kattintások és a külső input (lidar/érintőfal) is ezt hívja.
        /// </summary>
        void Activate();

        /// <summary>
        /// Az utolsó aktivitás időpontja (Time.time) — a PlayMaker ezt
        /// pollozza az idle-detektáláshoz (a kattintás és a külső input
        /// egyaránt frissíti).
        /// </summary>
        float LastActivationTime { get; }
    }
}
