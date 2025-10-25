namespace RSJWYFamework.Runtime
{
    public sealed class ComfyUIWebsocketEventArgs<TData> : EventArgsBase where TData : BaseComfyUImanager, new()
    {
        public ComfyUIWebsocketBaseMessage<TData> Data { get; set; } = null!;
    }
}