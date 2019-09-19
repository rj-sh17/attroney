using System.ComponentModel.DataAnnotations;

namespace AttorneyJournal.Models.HomeViewModels {
    public class SendFeedbackViewModel {
        /// <summary>
        ///     Some text.
        /// </summary>
        [Required]
        [Display (Name = "Feedback")]
        public string Feedback { get; set; }
    }
}