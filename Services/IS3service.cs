using Task = System.Threading.Tasks.Task;

namespace gestion_de_proyectos.Services
{
    /// <summary>
    /// Servicio para interactuar con AWS S3
    /// </summary>
    public interface IS3Service
    {
        /// <summary>
        /// Sube un archivo a S3
        /// </summary>
        /// <param name="stream">Stream del archivo</param>
        /// <param name="fileName">Nombre del archivo</param>
        /// <param name="folder">Carpeta en S3 (profile-images, project-files, etc.)</param>
        /// <param name="contentType">Content-Type del archivo</param>
        /// <returns>URL del archivo subido</returns>
        Task<string> UploadFileAsync(Stream stream, string fileName, string folder, string contentType);

        /// <summary>
        /// Genera una URL firmada para descargar un archivo privado
        /// </summary>
        /// <param name="fileKey">Clave del archivo en S3</param>
        /// <param name="expiresInMinutes">Minutos hasta que expire la URL</param>
        /// <returns>URL firmada</returns>
        Task<string> GetPresignedUrlAsync(string fileKey, int expiresInMinutes = 60);

        /// <summary>
        /// Elimina un archivo de S3
        /// </summary>
        /// <param name="fileKey">Clave del archivo en S3</param>
        Task DeleteFileAsync(string fileKey);

        /// <summary>
        /// Verifica si un archivo existe
        /// </summary>
        /// <param name="fileKey">Clave del archivo</param>
        /// <returns>True si existe</returns>
        Task<bool> FileExistsAsync(string fileKey);
    }
}