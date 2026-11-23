using Unity.Netcode.Components;

/// <summary>Animator sincronizado con autoridad del dueño.</summary>
public class ClientNetworkAnimator : NetworkAnimator
{
    protected override bool OnIsServerAuthoritative() => false;
}