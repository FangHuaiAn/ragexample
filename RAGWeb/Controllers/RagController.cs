using Microsoft.AspNetCore.Mvc;
using RAGWeb.Services;

namespace RAGWeb.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RagController : ControllerBase
    {
        private readonly RagService _ragService;

        public RagController(RagService ragService)
        {
            _ragService = ragService;
        }

        /// <summary>
        /// 問答：根據問題檢索 Reflections 內容並生成回答
        /// </summary>
        /// <param name="question">使用者問題</param>
        /// <returns>AI 回答</returns>
        [HttpPost("ask")]
        public async Task<IActionResult> Ask([FromBody] RagQuestionRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.Question))
                return BadRequest("Question is required.");

            var answer = await _ragService.AnswerQuestionAsync(request.Question);
            return Ok(new { answer });
        }
    }

    public class RagQuestionRequest
    {
        public string Question { get; set; }
    }
}
