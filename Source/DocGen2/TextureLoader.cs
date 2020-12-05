using System.IO;
using DirectXTexNet;

namespace Mal.DocGen2
{
    internal class TextureLoader
    {
        public ScratchImage LoadTextureScratch(string fileName)
        {
            if (!File.Exists(fileName))
                return null;
            ScratchImage scratch;
            try
            {
                switch (Path.GetExtension(fileName).ToUpper())
                {
                    case ".DDS":
                        scratch = TexHelper.Instance.LoadFromDDSFile(fileName, DDS_FLAGS.FORCE_RGB);
                        Decompress(ref scratch);
                        UnPremultiply(ref scratch);
                        break;

                    default:
                        scratch = TexHelper.Instance.LoadFromWICFile(fileName, WIC_FLAGS.FORCE_RGB);
                        break;
                }
            }
            catch (FileNotFoundException)
            {
                return null;
            }
            catch (DirectoryNotFoundException)
            {
                return null;
            }

            return scratch;
        }

        private void Decompress(ref ScratchImage scratch)
        {
            var meta = scratch.GetMetadata();
            ScratchImage decompressed;
            switch (meta.Format)
            {
                case DXGI_FORMAT.BC1_TYPELESS:
                case DXGI_FORMAT.BC1_UNORM:
                case DXGI_FORMAT.BC1_UNORM_SRGB:
                case DXGI_FORMAT.BC2_TYPELESS:
                case DXGI_FORMAT.BC2_UNORM:
                case DXGI_FORMAT.BC2_UNORM_SRGB:
                case DXGI_FORMAT.BC3_TYPELESS:
                case DXGI_FORMAT.BC3_UNORM:
                case DXGI_FORMAT.BC3_UNORM_SRGB:
                case DXGI_FORMAT.BC4_TYPELESS:
                case DXGI_FORMAT.BC4_UNORM:
                case DXGI_FORMAT.BC4_SNORM:
                case DXGI_FORMAT.BC5_TYPELESS:
                case DXGI_FORMAT.BC5_UNORM:
                case DXGI_FORMAT.BC5_SNORM:
                case DXGI_FORMAT.BC6H_TYPELESS:
                case DXGI_FORMAT.BC6H_UF16:
                case DXGI_FORMAT.BC6H_SF16:
                case DXGI_FORMAT.BC7_TYPELESS:
                case DXGI_FORMAT.BC7_UNORM:
                case DXGI_FORMAT.BC7_UNORM_SRGB:
                    decompressed = scratch.Decompress(DXGI_FORMAT.B8G8R8A8_UNORM);
                    break;
                default:
                    decompressed = scratch.Convert(DXGI_FORMAT.B8G8R8A8_UNORM, TEX_FILTER_FLAGS.FANT, 0.5f);
                    break;
            }

            scratch.Dispose();
            scratch = decompressed;
        }

        private void UnPremultiply(ref ScratchImage scratch)
        {
            var meta = scratch.GetMetadata();
            if (meta.GetAlphaMode() != TEX_ALPHA_MODE.PREMULTIPLIED)
                return;
            var nonPremultiplied = scratch.PremultiplyAlpha(TEX_PMALPHA_FLAGS.REVERSE);
            scratch.Dispose();
            scratch = nonPremultiplied;
        }
    }
}