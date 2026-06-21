using System.ComponentModel.DataAnnotations;

namespace GoKidAPI.DTO.Childs.Requests
{
    public class SubmitTaskRequest
    {
        /// <summary>
        /// ID الـ ChildTask (مش TaskTemplate)
        /// </summary>
        [Required(ErrorMessage = "Task ID is required")]
        public string TaskId { get; set; } = null!;

        /// <summary>
        /// ملف الصوت (صوت الطفل) - مطلوب لـ VoiceQuestion فقط
        /// </summary>
        public IFormFile? VoiceFile { get; set; }

        /// <summary>
        /// ملف الصورة (دليل المهمة) - مطلوب لـ EvidenceSubmission فقط
        /// </summary>
        public IFormFile? EvidenceFile { get; set; }

        /// <summary>
        /// اختياري: نص إضافي لو الطفل عايز يضيف تعليق (مثلاً للـ Parent)
        /// </summary>
        public string? Comment { get; set; }
    }
}
